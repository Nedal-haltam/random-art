using System.Diagnostics;
using System.Numerics;
using System.Text;
using static random_art.Program;
using Raylib_cs;
using Color = Raylib_cs.Color;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable RETURN0001
#pragma warning restore IDE0079 // Remove unnecessary suppression
namespace random_art
{
    public enum NodeType
    {
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
            INFO, WARNING, ERROR
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
                default: 
                    UNREACHABLE("Log");
                    return;
            }
            Console.Write(head + msg);
            Console.ForegroundColor = before;
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
#pragma warning disable IDE0066 // Convert switch statement to expression
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
#pragma warning restore IDE0066 // Convert switch statement to expression
        }
        static Node EvalUnary(Node expr, NodeType type)
        {
            switch (type)
            {
                case NodeType.SQRT: return NodeNumber(MathF.Sqrt(MathF.Abs(expr.number)));
                case NodeType.ADD:
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
        
        static Node GenNode(int branch, int depth)
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
                    return GenNode(1, depth - 1);
                if (t == 1)
                    return NodeADD(GenNode(2, depth - 1), GenNode(2, depth - 1));
                if (t == 2)
                    return NodeMUL(GenNode(2, depth - 1), GenNode(2, depth - 1));
            }
            UNREACHABLE("GenNode");
            return new();
        }
        static Node LoadFromGrammar(int depth)
        {
            return NodeTriple(GenNode(2, depth - 1), GenNode(2, depth - 1), GenNode(2, depth - 1));
        }
        static Node f;
        static Texture2D texture;
        static Texture2D? NextTexture;
        static readonly Texture2D DefaultTexture = new () { Id = 1, Height = 1, Width = 1, Mipmaps = 1, Format = PixelFormat.UncompressedR8G8B8A8 };
    static void UpdateTexture(ref Texture2D texture)
        {
            //f = NodeTriple(NodeX(), NodeY(), NodeNumber(0));
            f = LoadFromGrammar(10);
            //f = LoadBasicNode();
            NextTexture = GenerateTextureFromNode(f);
            if (NextTexture.HasValue)
                texture = NextTexture.Value;
            else
                UNREACHABLE("reijfk");
        }
        static void RenderTexture(ref Texture2D texture)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                UpdateTexture(ref texture);
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
                default:
                    UNREACHABLE("NodeToShaderFunction");
                    return new();
            }
        }
        static StringBuilder foo()
        {
            int depth = 15;
            StringBuilder fs = new StringBuilder();
            string func = NodeToShaderFunction(LoadFromGrammar(depth)).ToString();
            Console.WriteLine(func);
            fs.Append("#version 330\n");
            fs.Append("in vec2 fragTexCoord;\n");
            fs.Append("out vec4 finalColor;\n");
            fs.Append("void main()\n");
            fs.Append("{\n");
            fs.Append("float x = 2.0 * fragTexCoord.x - 1.0;\n");
            fs.Append("float y = 2.0 * fragTexCoord.y - 1.0;\n");
            fs.Append($"   vec3 tempcolor = {func};\n");
            fs.Append("    finalColor = vec4((tempcolor + 1) / 2.0, 1);\n");
            fs.Append("}");
            return fs;
        }
        static int Main(string[] args)
        {
            List<string> argslist = [.. args];

            // TODO:
            //- use command line arguments to support cli, gui
            //- in cli mode generate images using raylib textures
            //- You need a way to save and load the random function 
            //	- in a format so you can read it later and reuse it in the program
            //	- or same way of grammar handling
            //- You need a way to save and load the grammar (see if you can modify the code to add the grammar it self and then run it, after that go for the trivial approaches)
            //- A random grammar generator
            //	- you need rules for generation
            //- And for any of that to happen, you need a format (struct/class) for the grammars that is generated or possibly hardcoded

            //- try GPU to accelerate this function which does evaluate the function at each node `static StringBuilder EvalFunction(Node f, int start, int end)`
            //- try shaders and textures and generate the function in the fragment shader and add time and think of other things
            Raylib.InitWindow(WIDTH, HEIGHT, "Random Art");
            Raylib.SetTargetFPS(0);

            Shader s = new();
            s = Raylib.LoadShaderFromMemory(null, foo().ToString());

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);

                //RenderTexture(ref texture);

                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    Raylib.UnloadShader(s);
                    s = Raylib.LoadShaderFromMemory(null, foo().ToString());
                }
                Raylib.BeginShaderMode(s);
                Raylib.DrawTextureEx(DefaultTexture, new Vector2(0, 0), 0, WIDTH, Color.White);
                Raylib.EndShaderMode();

                Raylib.DrawFPS(0, 0);
                Raylib.EndDrawing();
            }
            Raylib.CloseWindow();
            return 0;
        }
    }
}
