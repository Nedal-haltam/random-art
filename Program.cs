using System.Numerics;
using System.Text;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;


#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable RETURN0001
#pragma warning restore IDE0079 // Remove unnecessary suppression
namespace random_art
{
    public enum NodeType
    {
        Number, random, X, Y, T, Boolean,
        SQRT,
        ADD, MUL, SUB, GT, GTE, MOD, DIV,
        Triple,
        If,
        Branch,
    }
    public sealed class NodeUnary
    {
        public NodeUnary() { }
        public NodeUnary(Node expr)
        {
            this.expr = expr;
        }
        public Node expr;
    }
    public sealed class NodeBinary
    {
        public NodeBinary(){}
        public NodeBinary(Node lhs, Node rhs)
        {
            this.lhs = lhs;
            this.rhs = rhs;
        }
        public Node lhs;
        public Node rhs;
    }
    public sealed class NodeTernary
    {
        public NodeTernary(){}
        public NodeTernary(Node first, Node second, Node third)
        {
            this.first = first;
            this.second = second;
            this.third = third;
        }
        public Node first;
        public Node second;
        public Node third;
    }
    public struct Node
    {
        public NodeType type;
        public float number;
        public bool boolean;
        public int branch;
        public NodeUnary unary;
        public NodeBinary binary;
        public NodeTernary ternary;
    }
    public struct Branch
    {
        public struct BranchNode(Node node, float prob)
        {
            public Node node = node;
            public float prob = prob;
        }
        public List<BranchNode> nodes;
    }
    public struct Grammar
    {
        public List<Branch> branches;
        public int startbranchindex;
        public List<int> terminalbranchindex;
    }
    internal sealed class Program
    {
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
            if (args.Length <= 0)
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
        static Node NodeBranch(int branch) => new() { type = NodeType.Branch, branch = branch };
        static Node NodeNumber(float number) => new() { type = NodeType.Number, number = number };
        static Node NodeBoolean(bool boolean) => new() { type = NodeType.Boolean, boolean = boolean };
        static Node NodeRandom() => new() { type = NodeType.random, number = random.NextSingle() * 2 - 1 };
        static Node NodeX() => new() { type = NodeType.X };
        static Node NodeY() => new() { type = NodeType.Y };
        static Node NodeT() => new() { type = NodeType.T };
        static Node NodeSQRT(Node expr) => NodeUnary(expr, NodeType.SQRT);
        static Node NodeADD(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.ADD);
        static Node NodeDIV(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.DIV);
        static Node NodeSUB(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.SUB);
        static Node NodeMUL(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.MUL);
        static Node NodeMOD(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.MOD);
        static Node NodeGT(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.GT);
        static Node NodeGTE(Node lhs, Node rhs) => NodeBinary(lhs, rhs, NodeType.GTE);
        static Node NodeIf(Node cond, Node then, Node elsee) => NodeTernary(cond, then, elsee, NodeType.If);
        static Node NodeTriple(Node first, Node second, Node third) => NodeTernary(first, second, third, NodeType.Triple);
        static Node NodeUnary(Node expr, NodeType type) => new() { type = type, unary = new(expr) };
        static Node NodeBinary(Node lhs, Node rhs, NodeType type) => new() { type = type, binary = new(lhs, rhs) };
        static Node NodeTernary(Node first, Node second, Node third, NodeType type) => new() { type = type, ternary = new(first, second, third) };
        static void NodePrint(ref Node node)
        {
            Console.Write(NodeToSb(ref node).ToString());
        }
        static void NodePrintln(ref Node node)
        {
            Console.WriteLine(NodeToSb(ref node).ToString());
        }
        static readonly Random random = new();
        static Grammar LoadDefaultGrammar()
        {
            return new()
            {
                branches =
                [
                    new() { nodes = [new() { node = NodeTriple(NodeBranch(2), NodeBranch(2), NodeBranch(2))}] },
                    new() { nodes =
                    [
                        new(NodeRandom(), 1),
                        new(NodeX(), 1),
                        new(NodeY(), 1),
                        new(NodeT(), 1),
                        new(NodeSQRT(NodeADD(NodeADD(NodeMUL(NodeX(), NodeX()),
                                 NodeMUL(NodeY(), NodeY())),
                                 NodeMUL(NodeT(), NodeT()))), 1),
                    ] },
                    new() { nodes = [new(NodeBranch(1), 1), new(NodeADD(NodeBranch(2), NodeBranch(2)), 1), new(NodeMUL(NodeBranch(2), NodeBranch(2)), 1)] },
                ],
                startbranchindex = 0,
                terminalbranchindex = [1],
            };
        }
        static Node GrammarToNode(Grammar grammar, Node node, int depth)
        {
            if (depth <= 0)
            {
                Branch b = grammar.branches[grammar.terminalbranchindex[random.Next(grammar.terminalbranchindex.Count)]];
                return BranchToNode(grammar, b.nodes[random.Next(b.nodes.Count)].node, 0);
            }
            else
            {
                Branch b = grammar.branches[node.branch];
                return BranchToNode(grammar, b.nodes[random.Next(b.nodes.Count)].node, depth - 1);
            }
        }
        static Node BranchToNode(Grammar grammar, Node node, int depth)
        {
            switch (node.type)
            {
                case NodeType.Branch:
                    return GrammarToNode(grammar, node, depth - 1);
                case NodeType.random:
                    return NodeNumber(node.number);
                case NodeType.Number:
                case NodeType.X:
                case NodeType.Y:
                case NodeType.Boolean:
                case NodeType.T:
                    return node;
                case NodeType.SQRT:
                    return NodeUnary(BranchToNode(grammar, node.unary.expr, depth), node.type);
                case NodeType.ADD:
                case NodeType.MUL:
                case NodeType.SUB:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.MOD:
                case NodeType.DIV:
                    return NodeBinary(BranchToNode(grammar, node.binary.lhs, depth), BranchToNode(grammar, node.binary.rhs, depth), node.type);
                case NodeType.Triple:
                case NodeType.If:
                    return NodeTernary(BranchToNode(grammar, node.ternary.first, depth), BranchToNode(grammar, node.ternary.second, depth), BranchToNode(grammar, node.ternary.third, depth), node.type);
                default:
                    UNREACHABLE("BranchToNode");
                    return new();
            }
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
                case NodeType.T:
                    return new("(t)");
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
                    return new($"(vec3({NodeToShaderFunction(f.ternary.first)}, {NodeToShaderFunction(f.ternary.second)}, {NodeToShaderFunction(f.ternary.third)}))");
                case NodeType.If:
                    return new($"(({NodeToShaderFunction(f.ternary.first)}) ? ({NodeToShaderFunction(f.ternary.second)}) : ({NodeToShaderFunction(f.ternary.third)}))");
                case NodeType.Branch:
                default:
                    UNREACHABLE("NodeToShaderFunction");
                    return new();
            }
        }
        static StringBuilder NodeToShader(Node f)
        {
            StringBuilder fs = new();
            string func = NodeToShaderFunction(f).ToString();
            fs.Append("#version 330\n");
            fs.Append("in vec2 fragTexCoord;\n");
            fs.Append("out vec4 finalColor;\n");
            fs.Append("uniform float csTIME;\n");
            fs.Append("void main()\n");
            fs.Append("{\n");
            fs.Append("float x = 2.0 * fragTexCoord.x - 1.0;\n");
            fs.Append("float y = 2.0 * fragTexCoord.y - 1.0;\n");
            fs.Append("float t = sin(csTIME);\n");
            fs.Append($"   vec3 tempcolor = {func};\n");
            fs.Append("    finalColor = vec4((tempcolor + 1) / 2.0, 1);\n");
            fs.Append('}');
            return fs;
        }
        static (Node, StringBuilder) GrammarToShader(Grammar grammar, int depth)
        {
            Node f = GrammarToNode(grammar, NodeBranch(grammar.startbranchindex), depth);
            return (f, NodeToShader(f));
        }
        public static Texture2D LoadDefaultTexture() => new() { Id = 1, Width = 1, Height = 1, Mipmaps = 1, Format = PixelFormat.UncompressedR8G8B8A8 };

        [RequiresUnreferencedCode("Calls random_art.Program.LoadOject<T>(String)")]
        static void Gui(int width, int height, int depth)
        {
            Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(width, height, "Random Art");
            Raylib.SetTargetFPS(60);
            Texture2D DefaultTexture = LoadDefaultTexture();
            //Grammar grammar = LoadDefaultGrammar();
            Grammar grammar = LoadOject<Grammar>("grammar");
            float time = 0;
            (Node node, StringBuilder fs) = GrammarToShader(grammar, depth);
            Shader s = Raylib.LoadShaderFromMemory(null, fs.ToString());
            Node currentnode = node;
            while (!Raylib.WindowShouldClose())
            {
                width = Raylib.GetScreenWidth();
                height = Raylib.GetScreenHeight();
                time += Raylib.GetFrameTime();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);
                Raylib.SetShaderValue(s, Raylib.GetShaderLocation(s, "csTIME"), time, ShaderUniformDataType.Float);
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    time = 0;
                    (Node tempnode, StringBuilder tempfs) = GrammarToShader(grammar, depth);
                    currentnode = tempnode;
                    Raylib.UnloadShader(s);
                    s = Raylib.LoadShaderFromMemory(null, tempfs.ToString());
                }
                if (Raylib.IsKeyPressed(KeyboardKey.S))
                {
                    string filepath = "Node.txt";
                    if (!NodeSave(filepath, currentnode))
                        Log(LogType.ERROR, $"could not save node because of : {ERROR_MESSAGE}\n");
                    else
                        Log(LogType.INFO, $"node saved successfully into : {filepath}\n");
                }
                if (Raylib.IsKeyPressed(KeyboardKey.L))
                {
                    time = 0;
                    string filepath = "Node.txt";
                    Node? tempnode = NodeLoad(filepath);
                    if (!tempnode.HasValue)
                    {
                        Log(LogType.ERROR, $"could not load node because of : {ERROR_MESSAGE}\n");
                    }
                    else
                    {
                        Log(LogType.INFO, $"node loaded successfully from : {filepath}\n");
                        Log(LogType.NORMAL, "compiling node into a fragment shader\n");
                        StringBuilder tempfs = NodeToShader(tempnode.Value);
                        currentnode = tempnode.Value;
                        Raylib.UnloadShader(s);
                        s = Raylib.LoadShaderFromMemory(null, tempfs.ToString());
                    }
                }
                Raylib.BeginShaderMode(s);
                Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, width, height), new(0, 0), 0, Color.White);
                Raylib.EndShaderMode();

                Raylib.DrawFPS(0, 0);
                Raylib.EndDrawing();
            }
            Raylib.UnloadShader(s);
            Raylib.CloseWindow();
        }
        static bool GrammarToImage(Grammar g, int width, int height, string filepath, int depth)
        {
            (Node _, StringBuilder shader) = GrammarToShader(g, depth);
            Texture2D DefaultTexture = LoadDefaultTexture();
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(width, height, "");
            Shader s = Raylib.LoadShaderFromMemory(null, shader.ToString());
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginShaderMode(s);
            Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, width, height), new(0, 0), 0, Color.White);
            Raylib.EndShaderMode();
            Image image = Raylib.LoadImageFromScreen();
            if (!Raylib.ExportImage(image, filepath))
            {
                ERROR_MESSAGE = $"Failed to export image to path: {filepath}\n";
                return false;
            }
            Raylib.EndDrawing();
            Raylib.UnloadShader(s);
            Raylib.UnloadImage(image);
            Raylib.CloseWindow();
            return true;
        }
        static bool NodeToImage(Node f, int width, int height, string filepath)
        {
            Texture2D DefaultTexture = LoadDefaultTexture();
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(width, height, "");
            Shader s = Raylib.LoadShaderFromMemory(null, NodeToShader(f).ToString());
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginShaderMode(s);
            Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, width, height), new(0, 0), 0, Color.White);
            Raylib.EndShaderMode();
            Image image = Raylib.LoadImageFromScreen();
            if (!Raylib.ExportImage(image, filepath))
            {
                ERROR_MESSAGE = $"Failed to export image to path: {filepath}\n";
                return false;
            }
            Raylib.EndDrawing();
            Raylib.UnloadShader(s);
            Raylib.UnloadImage(image);
            Raylib.CloseWindow();
            return true;
        }
        static void Usage()
        {
            // TODO: update usage to suite the features available
            Log(LogType.NORMAL, "\nUsage: \n");
            Log(LogType.NORMAL, $".\\random-art.exe [gui|cli] [option(s)]\n");
            Log(LogType.NORMAL, "\nOptions:\n");
            Log(LogType.NORMAL, $"\t{"-o <file>",-15} : place the output image into <file>\n");
            Log(LogType.NORMAL, $"\t{"-depth <depth>",-15} : specify the depth of the generated function\n\n");
        }
        static void Expr3d()
        {
            Raylib.InitWindow(800, 800, "raylib [core] example - 3d camera free");

            Camera3D camera;
            camera.Position = new Vector3(10.0f, 10.0f, 10.0f);
            camera.Target = new Vector3(0.0f, 0.0f, 0.0f);
            camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.FovY = 45.0f;
            camera.Projection = CameraProjection.Perspective;

            Vector3 cubePosition = new(0.0f, 0.0f, 0.0f);
            Raylib.SetTargetFPS(60);
            while (!Raylib.WindowShouldClose())
            {
                Raylib.UpdateCamera(ref camera, CameraMode.Free);
                if (Raylib.IsKeyDown(KeyboardKey.Z))
                {
                    camera.Target = new Vector3(0.0f, 0.0f, 0.0f);
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.RayWhite);

                Raylib.BeginMode3D(camera);
                Raylib.DrawPoint3D(cubePosition, Color.Brown);
                Raylib.DrawCircle3D(cubePosition, 7, new(0, 0, 0), 0, Color.Blue);
                Raylib.DrawCube(cubePosition, 2.0f, 2.0f, 2.0f, Color.Red);
                Raylib.DrawCubeWires(cubePosition, 2.0f, 2.0f, 2.0f, Color.Maroon);
                Raylib.DrawGrid(10, 1.0f);
                Raylib.EndMode3D();

                Raylib.EndDrawing();
            }
            Raylib.CloseWindow();
        }
        static StringBuilder NodeToSb(ref Node node)
        {
            StringBuilder sb = new();
            switch (node.type)
            {
                case NodeType.Number:
                case NodeType.random:
                    sb.Append(node.number);
                    break;
                case NodeType.X:
                    sb.Append('x');
                    break;
                case NodeType.Y:
                    sb.Append('y');
                    break;
                case NodeType.T:
                    sb.Append('t');
                    break;
                case NodeType.SQRT:
                    sb.Append($"{node.type}(");
                    sb.Append(NodeToSb(ref node.unary.expr));
                    sb.Append(')');
                    break;
                case NodeType.ADD:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.DIV:
                    sb.Append($"{node.type}(");
                    sb.Append(NodeToSb(ref node.binary.lhs));
                    sb.Append(',');
                    sb.Append(NodeToSb(ref node.binary.rhs));
                    sb.Append(')');
                    break;
                case NodeType.If:
                    sb.Append($"if(");
                    sb.Append(NodeToSb(ref node.ternary.first));
                    sb.Append(',');
                    sb.Append(NodeToSb(ref node.ternary.second));
                    sb.Append(',');
                    sb.Append(NodeToSb(ref node.ternary.third));
                    sb.Append(')');
                    break;
                case NodeType.Boolean:
                    sb.Append(node.boolean);
                    break;
                case NodeType.Triple:
                    sb.Append("triple(");
                    sb.Append(NodeToSb(ref node.ternary.first));
                    sb.Append(',');
                    sb.Append(NodeToSb(ref node.ternary.second));
                    sb.Append(',');
                    sb.Append(NodeToSb(ref node.ternary.third));
                    sb.Append(')');
                    break;
                case NodeType.Branch:
                    sb.Append($"{node.type}({node.branch})");
                    break;
                default:
                    UNREACHABLE("NodeToSb");
                    return new();
            }
            return sb;
        }
        static char? Peek(string src, ref int currindex, int offset = 0)
        {
            if (currindex + offset < src.Length)
            {
                return src[currindex + offset];
            }
            return null;
        }
        static char? Peek(char type, string src, ref int currindex, int offset = 0)
        {
            char? token = Peek(src, ref currindex, offset);
            if (token.HasValue && token.Value == type)
            {
                return token;
            }
            return null;
        }
        static char Consume(string src, ref int currindex)
        {
            return src.ElementAt(currindex++);
        }
        static char? Tryconsumeerr(char type, string src, ref int currindex)
        {
            if (Peek(type, src, ref currindex).HasValue)
            {
                return Consume(src, ref currindex);
            }
            Log(LogType.ERROR, $"Error Expected {type}\n");
            Environment.Exit(1);
            return null;
        }
        static NodeType StringToType(string token)
        {
            switch (token)
            {
                case "x":
                    return NodeType.X;
                case "y":
                    return NodeType.Y;
                case "t":
                    return NodeType.T;
                case "true":
                case "false":
                    return NodeType.Boolean;
                case "sqrt":
                    return NodeType.SQRT;
                case "add":
                    return NodeType.ADD;
                case "mul":
                    return NodeType.MUL;
                case "sub":
                    return NodeType.SUB;
                case "gt":
                    return NodeType.GT;
                case "gte":
                    return NodeType.GTE;
                case "mod":
                    return NodeType.MOD;
                case "div":
                    return NodeType.DIV;
                case "if":
                    return NodeType.If;
                case "triple":
                    return NodeType.Triple;
                case "random":
                    return NodeType.random;
                case "branch":
                    UNREACHABLE("StringToType");
                    return new();
                default:
                    return NodeType.Branch;
            }
        }
#pragma warning disable CS8629
        static Node TokenizeNode(string src, out int currindex)
        {
            // we may not have to take a substring whenever we want to tokenize, we can make it global and pass only the index, and that would be suffecient enough
            currindex = 0;
            StringBuilder buffer = new();
            while (Peek(src, ref currindex).HasValue)
            {
                while (Peek(src, ref currindex).HasValue && (char.IsAsciiLetterOrDigit(Peek(src, ref currindex).Value) || Peek(src, ref currindex).Value == '.' || Peek(src, ref currindex).Value == '-' || char.IsWhiteSpace(Peek(src, ref currindex).Value)))
                {
                    if (char.IsWhiteSpace(Peek(src, ref currindex).Value))
                        Consume(src, ref currindex);
                    else
                        buffer.Append(Consume(src, ref currindex));
                }
                string token = buffer.ToString().ToLower();
                //Number, random,
                if (float.TryParse(token, out float number))
                {
                    return NodeNumber(number);
                }
                //X, Y, T, Boolean
                //SQRT,
                //ADD, MUL, SUB, GT, GTE, MOD, DIV,
                //Triple,
                //If,
                //Branch,
                NodeType type = StringToType(token);
                switch (type)
                {
                    case NodeType.X:
                        return NodeX();
                    case NodeType.Y:
                        return NodeY();
                    case NodeType.T:
                        return NodeT();
                    case NodeType.Boolean:
                        return NodeBoolean(token == "true");
                    case NodeType.SQRT:
                        Tryconsumeerr('(', src, ref currindex);
                        Node expr = TokenizeNode(src[currindex..], out int exprstep);
                        currindex += exprstep;
                        Tryconsumeerr(')', src, ref currindex);
                        Node unary = NodeUnary(expr, type);
                        return unary;
                    case NodeType.ADD:
                    case NodeType.MUL:
                    case NodeType.SUB:
                    case NodeType.GT:
                    case NodeType.GTE:
                    case NodeType.MOD:
                    case NodeType.DIV:
                        Tryconsumeerr('(', src, ref currindex);
                        Node lhs = TokenizeNode(src[currindex..], out int lhsstep);
                        currindex += lhsstep;
                        Tryconsumeerr(',', src, ref currindex);
                        Node rhs = TokenizeNode(src[currindex..], out int rhsstep);
                        currindex += rhsstep;
                        Tryconsumeerr(')', src, ref currindex);
                        Node binary = NodeBinary(lhs, rhs, type);
                        return binary;
                    case NodeType.Triple:
                    case NodeType.If:
                        Tryconsumeerr('(', src, ref currindex);
                        Node first = TokenizeNode(src[currindex..], out int firststep);
                        currindex += firststep;
                        Tryconsumeerr(',', src, ref currindex);
                        Node second = TokenizeNode(src[currindex..], out int secondstep);
                        currindex += secondstep;
                        Tryconsumeerr(',', src, ref currindex);
                        Node third = TokenizeNode(src[currindex..], out int thirdstep);
                        currindex += thirdstep;
                        Tryconsumeerr(')', src, ref currindex);
                        Node ternary = NodeTernary(first, second, third, type);
                        return ternary;
                    case NodeType.Branch:
                        UNREACHABLE("TokenizeNode");
                        return new();
                    case NodeType.Number:
                    case NodeType.random:
                    default:
                        UNREACHABLE("TokenizeNode");
                        return new();
                }
            }
            UNREACHABLE("TokenizeNode");
            return new();
        }
        static (string, Branch) TokenizeBranch(string src, Dictionary<string, int> BranchTable)
        {
            throw new NotImplementedException();
        }
        static Grammar TokenizeToGrammar(string src)
        {
            //a grammar `g` is composed of multiple branches each branch `b` is composed of multiple nodes each node `n` has a priority(not exactly a probability) `p`
            throw new NotImplementedException();
        }
#pragma warning restore CS8629
        static bool NodeSave(string filepath, Node node)
        {
            StringBuilder sb = NodeToSb(ref node);
            try
            {
                File.WriteAllText(filepath, sb.ToString());
            }
            catch (Exception e)
            {
                ERROR_MESSAGE = e.Message;
                return false;
            }
            return true;
        }
        static Node? NodeLoad(string filepath)
        {
            string src;
            try
            {
                src = File.ReadAllText(filepath);
            }
            catch (Exception e)
            {
                ERROR_MESSAGE = e.Message;
                return null;
            }
            return TokenizeNode(src, out int _);
        }
        static string ERROR_MESSAGE = "";

        [RequiresUnreferencedCode("Calls System.Xml.Serialization.XmlSerializer.XmlSerializer(Type)")]
        public static void SaveObject<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter? writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                writer?.Close();
            }
        }
        [RequiresUnreferencedCode("Calls System.Xml.Serialization.XmlSerializer.XmlSerializer(Type)")]
        public static T LoadOject<T>(string filePath) where T : new()
        {
            TextReader? reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                object? o = serializer.Deserialize(reader);
                if (o == null)
                    return new();
                return (T)o;
            }
            finally
            {
                reader?.Close();
            }
        }

        [RequiresUnreferencedCode("Calls random_art.Program.SaveObject<T>(String, T, Boolean)")]
        static int Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Usage();
                Environment.Exit(0);
            }
            string mode = ShifArgs(ref args, "UNREACHABLE");

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
                            Log(LogType.ERROR, "Could not parse depth\n");
                    }
                    else
                    {
                        Log(LogType.ERROR, "invalid flag\n");
                    }
                }
                if (!GrammarToImage(LoadDefaultGrammar(), 800, 800, outputpath, depth))
                {
                    Log(LogType.ERROR, ERROR_MESSAGE);
                }
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
                            Log(LogType.ERROR, "Could not parse depth\n");
                    }
                    else
                    {
                        Log(LogType.ERROR, "invalid flag\n");
                    }
                }
                Gui(800, 800, depth);
            }
            else
            {
                Usage();
            }
            // TODO:
            //- naming
            //- add more flags (width, height, generate videos using ffmpeg pipeline)

            //- save and load grammar
            //- handle the branch in saving/loading the grammar 
            //- A random grammar generator
            //	- you need rules for generation
            //- we may have to redefine the binary operators (add, mul, ...) to take three inputs instead of just (lhs, rhs), and to expand it to a variadic for that matter
            return 0;
        }
    }
}