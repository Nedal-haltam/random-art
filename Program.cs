using System.Diagnostics;
using System.Numerics;
using System.Text;
using Raylib_cs;
using Color = Raylib_cs.Color;
using System;
using System.Xml.Xsl;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using Image = Raylib_cs.Image;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable RETURN0001
#pragma warning restore IDE0079 // Remove unnecessary suppression
namespace random_art
{
    public enum NodeType
    {
        Branch,

        Number, random, X, Y, Boolean,

        ADD, MUL, SUB, GT, GTE, MOD, DIV,

        SQRT,

        Triple,
        If,

    }
    public sealed class NodeBinary(Node lhs, Node rhs)
    {
        public Node lhs = lhs;
        public Node rhs = rhs;
    }
    public sealed class NodeUnary(Node expr)
    {
        public Node expr = expr;
    }
    public sealed class NodeTriple(Node first, Node second, Node third)
    {
        public Node first = first;
        public Node second = second;
        public Node third = third;
    }
    public sealed class NodeIf(Node cond, Node then, Node elsee)
    {
        public Node cond = cond;
        public Node then = then;
        public Node elsee = elsee;
    }
    public struct Node
    {
        public NodeType type;
        public float number;
        public bool boolean;
        public NodeUnary unary;
        public NodeBinary binary;
        public NodeTriple triple;
        public NodeIf iff;
    }
    public struct Branch
    {
        public struct BranchNode
        {
            public NodeType type;

            public Node node;
            public float probability;
            public int Branch;

            public int exprbranch;

            public int lhsbranch;
            public int rhsbranch;

            public int firstbranch;
            public int secondbranch;
            public int thirdbranch;

            public int iffbranch;
            public int thenbranch;
            public int elseebranch;

        }
        public List<BranchNode> nodes;
    }
    public struct Grammar
    {
        public List<Branch> branches;
        public int startbranchindex;
        public int terminalbranchindex;
    }
    internal sealed class Program
    {
        static Color ToColor(Vector3 v, float min = -1, float max = 1)
        {
            // min..max
            // 0..255
            return new(
                (byte)((v.X - min) * (255.0f / (max - min))),
                (byte)((v.Y - min) * (255.0f / (max - min))),
                (byte)((v.Z - min) * (255.0f / (max - min))));
        }

        public enum LogType
        {
            INFO, WARNING, ERROR, NORMAL
        }
        static void Log(LogType type, string msg)
        {
            ConsoleColor before = Console.ForegroundColor;
            string head;
            switch (type)
            {
                case LogType.INFO:
                    Console.ForegroundColor = ConsoleColor.Green;
                    head = "INFO: ";
                    break;
                case LogType.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    head = "WARNING: ";
                    break;
                case LogType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    head = "ERROR: ";
                    break;
                case LogType.NORMAL:
                    head = "";
                    break;
                default:
                    UNREACHABLE("Log");
                    return;
            }
            Console.Write(head + msg);
            Console.ForegroundColor = before;
        }
        static string ShifArgs(ref string[] args, string msg)
        {
            if (args.Length == 0)
            {
                Log(LogType.ERROR, msg);
                Environment.Exit(1);
            }
            string arg = args[0];
            args = args[1..];
            return arg;
        }
        static void UNREACHABLE(string msg)
        {
            Log(LogType.ERROR, $"UNREACHABLE: {msg}\n");
            Environment.Exit(1);
        }

        static Node NodeNumber(float number)
        {
            return new Node() { type = NodeType.Number, number = number };
        }
        static Node NodeBoolean(bool boolean)
        {
            return new Node() { type = NodeType.Boolean, boolean = boolean };
        }
        static Node NodeX()
        {
            return new Node() { type = NodeType.X };
        }
        static Node NodeRandom()
        {
            return new Node() { type = NodeType.random, number = r.NextSingle() * 2 - 1 };
        }
        static Node NodeY()
        {
            return new Node() { type = NodeType.Y };
        }
        static Node NodeSQRT(Node expr)
        {
            return new Node() { type = NodeType.SQRT, unary = new(expr) };
        }
        static Node NodeADD(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.ADD, binary = new(lhs, rhs) };
        }
        static Node NodeDIV(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.DIV, binary = new(lhs, rhs) };
        }
        static Node NodeSUB(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.SUB, binary = new(lhs, rhs) };
        }
        static Node NodeMUL(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.MUL, binary = new(lhs, rhs) };
        }
        static Node NodeMOD(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.MOD, binary = new(lhs, rhs) };
        }


        static Node NodeGT(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.GT, binary = new(lhs, rhs) };
        }
        static Node NodeGTE(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.GTE, binary = new(lhs, rhs) };
        }

        static Node NodeIf(Node cond, Node then, Node elsee)
        {
            return new() { type = NodeType.If, iff = new(cond, then, elsee) };
        }
        static Node NodeTriple(Node first, Node second, Node third)
        {
            return new Node() { type = NodeType.Triple, triple = new(first, second, third) };
        }
        static Node EvalBinary(Node lhs, Node rhs, NodeType type)
        {
            switch (type)
            {
                case NodeType.ADD: return NodeNumber(lhs.number + rhs.number);
                case NodeType.SUB: return NodeNumber(lhs.number - rhs.number);
                case NodeType.MUL: return NodeNumber(lhs.number * rhs.number);
                case NodeType.MOD: return NodeNumber(lhs.number % rhs.number);
                case NodeType.GT: return NodeBoolean(lhs.number > rhs.number);
                case NodeType.GTE: return NodeBoolean(lhs.number >= rhs.number);
                case NodeType.DIV: return NodeNumber((rhs.number == 0) ? lhs.number : lhs.number / rhs.number);
                case NodeType.SQRT:
                case NodeType.Branch:
                case NodeType.Number:
                case NodeType.random:
                case NodeType.Boolean:
                case NodeType.X:
                case NodeType.Y:
                case NodeType.If:
                case NodeType.Triple:
                default:
                    UNREACHABLE("EvalBinary");
                    return new();
            };
        }
        static Node EvalUnary(Node expr, NodeType type)
        {
            switch (type)
            {
                case NodeType.SQRT: return NodeNumber(MathF.Sqrt(MathF.Abs(expr.number)));
                case NodeType.ADD:
                case NodeType.Branch:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.DIV:
                case NodeType.Number:
                case NodeType.random:
                case NodeType.Boolean:
                case NodeType.X:
                case NodeType.Y:
                case NodeType.If:
                case NodeType.Triple:
                default:
                    UNREACHABLE("EvalUnary");
                    return new();
            }
        }
        static Node? EvalToNode(ref Node f, float x, float y)
        {
            switch (f.type)
            {
                case NodeType.random: return NodeNumber(f.number);
                case NodeType.Number:
                case NodeType.Boolean: return f;
                case NodeType.X: return NodeNumber(x);
                case NodeType.Y: return NodeNumber(y);
                case NodeType.SQRT:
                    Node? expr = EvalToNode(ref f.unary.expr, x, y);
                    if (!expr.HasValue) return null;
                    if (expr.Value.type != NodeType.Number) return null;
                    return EvalUnary(expr.Value, f.type);
                case NodeType.ADD:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.DIV:
                    Node? lhs = EvalToNode(ref f.binary.lhs, x, y);
                    if (!lhs.HasValue) return null;
                    if (lhs.Value.type != NodeType.Number) return null;
                    Node? rhs = EvalToNode(ref f.binary.rhs, x, y);
                    if (!rhs.HasValue) return null;
                    if (rhs.Value.type != NodeType.Number) return null;
                    return EvalBinary(lhs.Value, rhs.Value, f.type);
                case NodeType.If:
                    Node? cond = EvalToNode(ref f.iff.cond, x, y);
                    if (!cond.HasValue) return null;
                    if (cond.Value.type != NodeType.Boolean) return null;
                    if (cond.Value.boolean)
                    {
                        Node? then = EvalToNode(ref f.iff.then, x, y);
                        if (!then.HasValue) return null;
                        return then.Value;
                    }
                    else
                    {
                        Node? elsee = EvalToNode(ref f.iff.elsee, x, y);
                        if (!elsee.HasValue) return null;
                        return elsee.Value;
                    }
                case NodeType.Triple:
                    Node? first = EvalToNode(ref f.triple.first, x, y);
                    if (!first.HasValue) return null;
                    if (first.Value.type != NodeType.Number) return null;
                    Node? second = EvalToNode(ref f.triple.second, x, y);
                    if (!second.HasValue) return null;
                    if (second.Value.type != NodeType.Number) return null;
                    Node? third = EvalToNode(ref f.triple.third, x, y);
                    if (!third.HasValue) return null;
                    if (third.Value.type != NodeType.Number) return null;
                    return NodeTriple(NodeNumber(first.Value.number), NodeNumber(second.Value.number), NodeNumber(third.Value.number));
                case NodeType.Branch:
                default:
                    UNREACHABLE("EvalToNode");
                    return new();
            }
        }
        static Color? Eval(ref Node f, float x, float y, float min = -1, float max = 1)
        {
            Node? c = EvalToNode(ref f, x, y);

            if (!c.HasValue)
                return null;
            if (c.Value.type != NodeType.Triple)
                return null;
            if (c.Value.triple.first.type != NodeType.Number)
                return null;
            if (c.Value.triple.second.type != NodeType.Number)
                return null;
            if (c.Value.triple.third.type != NodeType.Number)
                return null;

            return ToColor(new(c.Value.triple.first.number, c.Value.triple.second.number, c.Value.triple.third.number), min, max);
        }
        const int taskCount = 10;
        const int WIDTH = 10 * 80;
        const int HEIGHT = 10 * 80;
        static bool GeneratePPM(string FilePath, Func<float, float, Color> f)
        {
            StringBuilder image = new();
            image.Append($"P3\n{WIDTH} {HEIGHT}\n255\n");
            for (int y = 0; y < HEIGHT; ++y)
            {
                float Normalizedy = ((float)y / HEIGHT) * 2 - 1;
                for (int x = 0; x < WIDTH; ++x)
                {
                    float Normalizedx = ((float)x / WIDTH) * 2 - 1;
                    Color c = f(Normalizedx, Normalizedy);
                    image.Append($"{c.R} {c.G} {c.B}\n");
                }
            }
            File.WriteAllText(FilePath, image.ToString());
            return true;
        }
        static Texture2D? GenerateTextureFromNode(Node f)
        {
            float min = -1;
            float max = 1;
            RenderTexture2D texture = Raylib.LoadRenderTexture(WIDTH, HEIGHT);
            Raylib.UnloadRenderTexture(texture);
            texture = Raylib.LoadRenderTexture(WIDTH, HEIGHT);
            Raylib.BeginTextureMode(texture);
            Raylib.ClearBackground(Color.White);
            for (int y = 0; y < HEIGHT; ++y)
            {
                //float Normalizedy = ((float)y / HEIGHT);
                float Normalizedy = ((float)y / HEIGHT) * 2 - 1;
                for (int x = 0; x < WIDTH; ++x)
                {
                    float Normalizedx = ((float)x / WIDTH) * 2 - 1;
                    //float Normalizedx = ((float)x / WIDTH);
                    Color? c = Eval(ref f, Normalizedx, Normalizedy, min, max);
                    if (!c.HasValue)
                        return null;
                    Raylib.DrawPixel(x, y, c.Value);
                }
            }
            Raylib.EndTextureMode();
            return texture.Texture;
        }
        static void NodeTriplePrint(ref NodeTriple triple)
        {
            Console.Write("triple(");
            NodePrint(ref triple.first);
            Console.Write(", ");
            NodePrint(ref triple.second);
            Console.Write(", ");
            NodePrint(ref triple.third);
            Console.Write(")");
        }
        static void NodePrint(ref Node node)
        {
            switch (node.type)
            {
                case NodeType.Number:
                case NodeType.random:
                    Console.Write(node.number);
                    break;
                case NodeType.X:
                    Console.Write("x");
                    break;
                case NodeType.Y:
                    Console.Write("y");
                    break;
                case NodeType.SQRT:
                    Console.Write($"{node.type}(");
                    NodePrint(ref node.unary.expr);
                    Console.Write(")");
                    break;
                case NodeType.ADD:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.DIV:
                    Console.Write($"{node.type}(");
                    NodePrint(ref node.binary.lhs);
                    Console.Write(", ");
                    NodePrint(ref node.binary.rhs);
                    Console.Write(")");
                    break;
                case NodeType.If:
                    Console.Write($"if (");
                    NodePrint(ref node.iff.cond);
                    Console.Write(") ");
                    Console.Write("then (");
                    NodePrint(ref node.iff.then);
                    Console.Write(") ");
                    Console.Write("else (");
                    NodePrint(ref node.iff.elsee);
                    Console.Write(")");
                    break;
                case NodeType.Boolean:
                    Console.Write(node.boolean);
                    break;
                case NodeType.Triple:
                    NodeTriplePrint(ref node.triple);
                    break;
                case NodeType.Branch:
                default:
                    UNREACHABLE("NodePrint");
                    return;
            }
        }
        static void NodePrintln(ref Node node)
        {
            NodePrint(ref node);
            Console.WriteLine();
        }
        static readonly Random r = new();
        static Node LoadBasicNode()
        {
            return NodeIf(
                NodeGTE(NodeMUL(NodeX(), NodeY()), NodeNumber(0)),
                NodeTriple(NodeX(), NodeY(), NodeNumber(1)),
                NodeTriple(NodeMOD(NodeX(), NodeY()), NodeMOD(NodeX(), NodeY()), NodeMOD(NodeX(), NodeY()))
                );
        }

        static Node BasicGrammar(int branch, int depth)
        {
            if (depth == 0)
            {
                int t = r.Next(3);
                if (t == 0)
                    return NodeRandom();
                if (t == 1)
                    return NodeX();
                if (t == 2)
                    return NodeY();
            }
            if (branch == 1)
            {
                int t = r.Next(3);
                if (t == 0)
                    return NodeRandom();
                if (t == 1)
                    return NodeX();
                if (t == 2)
                    return NodeY();
            }
            else if (branch == 2)
            {
                int t = r.Next(3);
                if (t == 0)
                    return BasicGrammar(1, depth - 1);
                if (t == 1)
                    return NodeADD(BasicGrammar(2, depth - 1), BasicGrammar(2, depth - 1));
                if (t == 2)
                    return NodeMUL(BasicGrammar(2, depth - 1), BasicGrammar(2, depth - 1));
            }
            UNREACHABLE("BasicGrammar");
            return new();
        }
        static Node GeneratNodeFromFunctionalGrammar(int depth, Func<int , int, Node> FunctionGrammar)
        {
            return NodeTriple(FunctionGrammar(2, depth - 1), FunctionGrammar(2, depth - 1), FunctionGrammar(2, depth - 1));
        }
        static Texture2D texture;
        static Texture2D? NextTexture;
        static readonly Texture2D DefaultTexture = new() { Id = 1, Height = 1, Width = 1, Mipmaps = 1, Format = PixelFormat.UncompressedR8G8B8A8 };
        static void UpdateTexture(ref Texture2D texture, Grammar grammar, int depth)
        {
            Node f = GrammarToNode(grammar, 0, depth);
            NextTexture = GenerateTextureFromNode(f);
            if (NextTexture.HasValue)
                texture = NextTexture.Value;
            else
                UNREACHABLE("UpdateTexture");
        }
        static void RenderTexture(ref Texture2D texture, Grammar grammar, int depth)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                UpdateTexture(ref texture, grammar, depth);
            }
            Raylib.DrawTextureRec(texture, new() { X = 0, Y = 0, Width = WIDTH, Height = -HEIGHT }, new() { X = 0, Y = 0 }, Color.White);
        }
        static StringBuilder NodeToShaderFunction(Node f)
        {
            switch (f.type)
            {
                case NodeType.Number:
                case NodeType.random:
                    return new($"({f.number})");
                case NodeType.X:
                    return new("(x)");
                case NodeType.Y:
                    return new("(y)");
                case NodeType.Boolean:
                    return new((f.boolean) ? "(true)" : "(false)");
                case NodeType.ADD:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} + {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.MUL:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} * {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.SUB:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} - {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.GT:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} > {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.GTE:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} >= {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.MOD:
                    return new($"mod({NodeToShaderFunction(f.binary.lhs)}, {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.DIV:
                    return new($"({NodeToShaderFunction(f.binary.lhs)} / {NodeToShaderFunction(f.binary.rhs)})");
                case NodeType.SQRT:
                    return new($"(sqrt({NodeToShaderFunction(f.unary.expr)}))");
                case NodeType.Triple:
                    return new($"(vec3({NodeToShaderFunction(f.triple.first)}, {NodeToShaderFunction(f.triple.second)}, {NodeToShaderFunction(f.triple.third)}))");
                case NodeType.If:
                    return new($"({NodeToShaderFunction(f.iff.cond)}) ? ({NodeToShaderFunction(f.iff.then)}) : ({NodeToShaderFunction(f.iff.elsee)})");
                case NodeType.Branch:
                default:
                    UNREACHABLE("NodeToShaderFunction");
                    return new();
            }
        }
        static Grammar LoadDefaultGrammar()
        {
            return new()
            {
                branches = 
                [
                    new() { nodes = [new() { type = NodeType.Triple, firstbranch = 2, secondbranch = 2, thenbranch = 2}] },
                    new() { nodes = [new() { type = NodeType.random}, new() { type = NodeType.X}, new() { type = NodeType.Y}] },
                    new() { nodes = [new() { type = NodeType.Branch, Branch = 1}, new() { type = NodeType.ADD, lhsbranch = 2, rhsbranch = 2}, new() { type = NodeType.MUL, lhsbranch = 2, rhsbranch = 2 }] },
                ],
                startbranchindex = 0,
                terminalbranchindex = 1,
            };
        }
        static Node GrammarToNode(Grammar grammar, int branch, int depth)
        {
            Branch b;
            int next;
            Branch.BranchNode branchnode;
            if (depth == 0)
            {
                b = grammar.branches[grammar.terminalbranchindex];
                next = r.Next(b.nodes.Count);
                branchnode = b.nodes[next];
            }
            else
            {
                b = grammar.branches[branch];
                next = r.Next(b.nodes.Count);
                branchnode = b.nodes[next];
            }
            switch (branchnode.type)
            {

                case NodeType.Branch:
                    return GrammarToNode(grammar, branchnode.Branch, depth - 1);
                case NodeType.Number: 
                    return NodeNumber(branchnode.node.number);
                case NodeType.random:
                    return NodeRandom();
                case NodeType.X:
                    return NodeX();
                case NodeType.Y:
                    return NodeY();
                case NodeType.Boolean: 
                    return NodeBoolean(branchnode.node.boolean);
                case NodeType.ADD:
                    return NodeADD(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.MUL: 
                    return NodeMUL(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.SUB: 
                    return NodeSUB(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.GT: 
                    return NodeGT(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.GTE: 
                    return NodeGTE(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.MOD: 
                    return NodeMOD(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.DIV: 
                    return NodeDIV(GrammarToNode(grammar, branchnode.lhsbranch, depth - 1), GrammarToNode(grammar, branchnode.rhsbranch, depth - 1));
                case NodeType.SQRT: 
                    return NodeSQRT(GrammarToNode(grammar, branchnode.exprbranch, depth - 1));
                case NodeType.Triple:
                    return NodeTriple(GrammarToNode(grammar, branchnode.firstbranch, depth - 1), GrammarToNode(grammar, branchnode.secondbranch, depth - 1), GrammarToNode(grammar, branchnode.thirdbranch, depth - 1));
                case NodeType.If:
                    return NodeIf(GrammarToNode(grammar, branchnode.iffbranch, depth - 1), GrammarToNode(grammar, branchnode.thenbranch, depth - 1), GrammarToNode(grammar, branchnode.elseebranch, depth - 1));
                default:
                    UNREACHABLE("GrammarToNode");
                    return new();
            }
        }
        static StringBuilder GrammarToShaderFunction(Grammar grammar, int depth)
        {
            Node f = GrammarToNode(grammar, grammar.startbranchindex, depth);
            StringBuilder fs = new();
            string func = NodeToShaderFunction(f).ToString();
            fs.Append("#version 330\n");
            fs.Append("in vec2 fragTexCoord;\n");
            fs.Append("out vec4 finalColor;\n");
            fs.Append("void main()\n");
            fs.Append("{\n");
            fs.Append("float x = 2.0 * fragTexCoord.x - 1.0;\n");
            fs.Append("float y = 2.0 * fragTexCoord.y - 1.0;\n");
            fs.Append($"   vec3 tempcolor = {func};\n");
            fs.Append("    finalColor = vec4((tempcolor + 1) / 2.0, 1);\n");
            fs.Append('}');
            return fs;
        }
        static void Gui(int depth)
        {
            Raylib.InitWindow(WIDTH, HEIGHT, "Random Art");
            Raylib.SetTargetFPS(0);
            Grammar grammar = LoadDefaultGrammar();
            Shader s = Raylib.LoadShaderFromMemory(null, GrammarToShaderFunction(grammar, depth).ToString());
            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    Raylib.UnloadShader(s);
                    s = Raylib.LoadShaderFromMemory(null, GrammarToShaderFunction(grammar, depth).ToString());
                }
                Raylib.BeginShaderMode(s);
                Raylib.DrawTextureEx(DefaultTexture, new Vector2(0, 0), 0, WIDTH, Color.White);
                Raylib.EndShaderMode();

                Raylib.DrawFPS(0, 0);
                Raylib.EndDrawing();
            }
            Raylib.UnloadShader(s);
            Raylib.CloseWindow();
        }
        static void Usage()
        {
            // TODO: update usage to suite the features available
            Log(LogType.NORMAL, "\nUsage: \n");
            Log(LogType.NORMAL, $".\\random-art.exe [gui|cli] [option(s)]\n");
            Log(LogType.NORMAL, "\nOptions:\n");
            Log(LogType.NORMAL, $"\t{"-o <file>", -15} : place the output image into <file>\n");
            Log(LogType.NORMAL, $"\t{"-depth <depth>", -15} : specify the depth of the generated function\n\n");
        }
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                Environment.Exit(0);
            }
            
            string mode = ShifArgs(ref args, "No mode provided\n");

            if (mode.Equals("-h", StringComparison.CurrentCultureIgnoreCase)) { Usage(); Environment.Exit(0); }

            if (mode.Equals("cli", StringComparison.CurrentCultureIgnoreCase))
            {
                string outputpath = "Default_output_path.png";
                int depth = 20;
                while (args.Length > 0)
                {
                    string flag = ShifArgs(ref args, "");
                    if (flag == "-o")
                    {
                        outputpath = ShifArgs(ref args, "No output file path provided\n");
                    }
                    else if (flag == "-depth")
                    {
                        string d = ShifArgs(ref args, "No depth provided\n");
                        if (!int.TryParse(d, out depth))
                            Log(LogType.ERROR, "Could not parse depth");
                    }
                }
                Grammar grammar = LoadDefaultGrammar();
                Raylib.InitWindow(WIDTH, HEIGHT, "");
                Shader s = Raylib.LoadShaderFromMemory(null, GrammarToShaderFunction(grammar, depth).ToString());
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);
                Raylib.BeginShaderMode(s);
                Raylib.DrawTextureEx(DefaultTexture, new Vector2(0, 0), 0, WIDTH, Color.White);
                Raylib.EndShaderMode();
                Image image = Raylib.LoadImageFromScreen();
                if (!Raylib.ExportImage(image, outputpath))
                {
                    Log(LogType.ERROR, $"Failed to export image to path: {outputpath}\n");
                }
                Raylib.EndDrawing();
                Raylib.UnloadShader(s);
                Raylib.UnloadImage(image);
                Raylib.CloseWindow();
            }
            else if (mode.Equals("gui", StringComparison.CurrentCultureIgnoreCase))
            {
                int depth = 20;
                while (args.Length > 0)
                {
                    string flag = ShifArgs(ref args, "");
                    if (flag == "-depth")
                    {
                        string d = ShifArgs(ref args, "No depth provided\n");
                        if (!int.TryParse(d, out depth))
                            Log(LogType.ERROR, "Could not parse depth");
                    }
                }
                Gui(depth);
            }
            else
            {
                Usage();
                Environment.Exit(0);
            }
            // TODO:
            //- You need a way to save and load the random function 
            //	- in a format so you can read it later and reuse it in the program
            //	- or same way of grammar handling
            //- You need a way to save and load the grammar
            //  (see if you can modify the code to add the grammar it self and then run it, after that go for the trivial approaches)
            //- A random grammar generator
            //	- you need rules for generation

            //- try GPU to accelerate this function which does evaluate the function at each node (or the equivalent)
            //  `static StringBuilder EvalFunction(Node f, int start, int end)`
            //- add time and think of other things
            return 0;
        }
    }
}
