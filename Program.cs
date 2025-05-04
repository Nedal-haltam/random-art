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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.ComponentModel;

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
    public sealed class NodeUnary(Node expr)
    {
        public Node expr = expr;
    }
    public sealed class NodeBinary(Node lhs, Node rhs)
    {
        public Node lhs = lhs;
        public Node rhs = rhs;
    }
    public sealed class NodeTernary(Node first, Node second, Node third)
    {
        public Node first = first;
        public Node second = second;
        public Node third = third;
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
        public static class FunctionalRandomArt
        {
            static Color ToColor(Vector3 v, float min, float max) =>
                new((byte)((v.X - min) * (255.0f / (max - min))),
                    (byte)((v.Y - min) * (255.0f / (max - min))),
                    (byte)((v.Z - min) * (255.0f / (max - min))));
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
                    case NodeType.DIV: return NodeNumber((rhs.number <= 1e-8) ? lhs.number : lhs.number / rhs.number);
                    case NodeType.SQRT:
                    case NodeType.Branch:
                    case NodeType.Number:
                    case NodeType.random:
                    case NodeType.Boolean:
                    case NodeType.X:
                    case NodeType.Y:
                    case NodeType.T:
                    case NodeType.If:
                    case NodeType.Triple:
                    default:
                        UNREACHABLE("EvalBinary");
                        return new();
                }
            }
            static Node EvalUnary(Node expr, NodeType type)
            {
                switch (type)
                {
                    case NodeType.SQRT: return NodeNumber(MathF.Sqrt(expr.number));
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
                    case NodeType.T:
                    case NodeType.If:
                    case NodeType.Triple:
                    default:
                        UNREACHABLE("EvalUnary");
                        return new();
                }
            }
            static Node? EvalToNode(ref Node f, float x, float y, float t)
            {
                switch (f.type)
                {
                    case NodeType.random: return NodeNumber(f.number);
                    case NodeType.Number:
                    case NodeType.Boolean: return f;
                    case NodeType.X: return NodeNumber(x);
                    case NodeType.Y: return NodeNumber(y);
                    case NodeType.T: return NodeNumber(t);
                    case NodeType.SQRT:
                        Node? expr = EvalToNode(ref f.unary.expr, x, y, t);
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
                        Node? lhs = EvalToNode(ref f.binary.lhs, x, y, t);
                        if (!lhs.HasValue) return null;
                        if (lhs.Value.type != NodeType.Number) return null;
                        Node? rhs = EvalToNode(ref f.binary.rhs, x, y, t);
                        if (!rhs.HasValue) return null;
                        if (rhs.Value.type != NodeType.Number) return null;
                        return EvalBinary(lhs.Value, rhs.Value, f.type);
                    case NodeType.If:
                        Node? cond = EvalToNode(ref f.ternary.first, x, y, t);
                        if (!cond.HasValue) return null;
                        if (cond.Value.type != NodeType.Boolean) return null;
                        if (cond.Value.boolean)
                        {
                            Node? then = EvalToNode(ref f.ternary.second, x, y, t);
                            if (!then.HasValue) return null;
                            return then.Value;
                        }
                        else
                        {
                            Node? elsee = EvalToNode(ref f.ternary.third, x, y, t);
                            if (!elsee.HasValue) return null;
                            return elsee.Value;
                        }
                    case NodeType.Triple:
                        Node? first = EvalToNode(ref f.ternary.first, x, y, t);
                        if (!first.HasValue) return null;
                        if (first.Value.type != NodeType.Number) return null;
                        Node? second = EvalToNode(ref f.ternary.second, x, y, t);
                        if (!second.HasValue) return null;
                        if (second.Value.type != NodeType.Number) return null;
                        Node? third = EvalToNode(ref f.ternary.third, x, y, t);
                        if (!third.HasValue) return null;
                        if (third.Value.type != NodeType.Number) return null;
                        return NodeTriple(NodeNumber(first.Value.number), NodeNumber(second.Value.number), NodeNumber(third.Value.number));
                    case NodeType.Branch:
                    default:
                        UNREACHABLE("EvalToNode");
                        return new();
                }
            }
            static Color? Eval(ref Node f, float x, float y, float t, float min, float max)
            {
                Node? c = EvalToNode(ref f, x, y, t);

                if (!c.HasValue)
                    return null;
                if (c.Value.type != NodeType.Triple)
                    return null;
                if (c.Value.ternary.first.type != NodeType.Number)
                    return null;
                if (c.Value.ternary.second.type != NodeType.Number)
                    return null;
                if (c.Value.ternary.third.type != NodeType.Number)
                    return null;

                return ToColor(new(c.Value.ternary.first.number, c.Value.ternary.second.number, c.Value.ternary.third.number), min, max);
            }

            static Texture2D? GenerateTextureFromNode(Node f, int width, int height, float time)
            {
                float min = -1;
                float max = 1;
                Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
                Raylib.InitWindow(width, height, "");
                RenderTexture2D texture = Raylib.LoadRenderTexture(width, height);
                Raylib.BeginTextureMode(texture);
                Raylib.ClearBackground(Color.White);
                for (int y = 0; y < height; ++y)
                {
                    float Normalizedy = ((float)y / height) * 2 - 1;
                    for (int x = 0; x < width; ++x)
                    {
                        float Normalizedx = ((float)x / width) * 2 - 1;
                        Color? c = Eval(ref f, Normalizedx, Normalizedy, time, min, max);
                        if (!c.HasValue)
                            return null;
                        Raylib.DrawPixel(x, y, c.Value);
                    }
                }
                Raylib.EndTextureMode();
                Raylib.CloseWindow();
                return texture.Texture;
            }
            static void UpdateTexture(ref Texture2D texture, Grammar grammar, int width, int height, int depth, int time)
            {
                Node f = GrammarToNode(grammar, NodeBranch(grammar.startbranchindex), depth);
                Texture2D? NextTexture = GenerateTextureFromNode(f, width, height, time);
                if (NextTexture.HasValue)
                    texture = NextTexture.Value;
                else
                    UNREACHABLE("UpdateTexture");
            }
            public static bool GeneratePNGFromNode(Node f, string filepath, int width, int height, int time)
            {
                Texture2D? texture = GenerateTextureFromNode(f, width, height, time);
                if (!texture.HasValue)
                    return false;
                Image image = Raylib.LoadImageFromTexture(texture.Value);
                if (!Raylib.ExportImage(image, filepath))
                    return false;
                return true;
            }
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
        static void Gui(int depth)
        {
            int width = 800;
            int height = 800;
            Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(width, height, "Random Art");
            Raylib.SetTargetFPS(60);
            Texture2D DefaultTexture = LoadDefaultTexture();
            Grammar grammar = LoadDefaultGrammar();
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
                    NodeSave($"Node.txt", currentnode);
                }
                if (Raylib.IsKeyPressed(KeyboardKey.L))
                {
                    time = 0;
                    Node tempnode = NodeLoad("Node.txt");
                    StringBuilder tempfs = NodeToShader(tempnode);
                    currentnode = tempnode;
                    Raylib.UnloadShader(s);
                    s = Raylib.LoadShaderFromMemory(null, tempfs.ToString());
                }
                Raylib.BeginShaderMode(s);
                Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, width, height), new(0, 0), 0, Color.White);
                Raylib.EndShaderMode();

                Raylib.DrawFPS(0, 0);
                Raylib.EndDrawing();
            }
            Raylib.UnloadShader(s);
            Raylib.CloseWindow();
            Environment.Exit(0);
        }
        static void Cli(string filepath, int depth)
        {
            int width = 800;
            int height = 800;
            Texture2D DefaultTexture = LoadDefaultTexture();
            Grammar grammar = LoadDefaultGrammar();
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(width, height, "");
            Shader s = Raylib.LoadShaderFromMemory(null, GrammarToShader(grammar, depth).ToString());
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginShaderMode(s);
            Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, width, height), new(0, 0), 0, Color.White);
            Raylib.EndShaderMode();
            Image image = Raylib.LoadImageFromScreen();
            if (!Raylib.ExportImage(image, filepath))
            {
                Log(LogType.ERROR, $"Failed to export image to path: {filepath}\n");
            }
            Raylib.EndDrawing();
            Raylib.UnloadShader(s);
            Raylib.UnloadImage(image);
            Raylib.CloseWindow();
            Environment.Exit(0);
        }
        static void NodeToImage(Node f, int width, int height, string filepath)
        {
            Texture2D DefaultTexture = LoadDefaultTexture();
            Grammar grammar = LoadDefaultGrammar();
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
                Log(LogType.ERROR, $"Failed to export image to path: {filepath}\n");
            }
            Raylib.EndDrawing();
            Raylib.UnloadShader(s);
            Raylib.UnloadImage(image);
            Raylib.CloseWindow();
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
        static void expr3d()
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
        static Node LoadAllNodes(bool cond)
        {
            float r;
            if (cond)
                r = -0.65136623f;
            else
                r = -0.48348713f;
            //TODO: handle the branch in saving/loading the grammar 
            return NodeIf(
                NodeGTE(NodeMUL(NodeX(), NodeY()), NodeIf(NodeGT(NodeX(), NodeNumber(0)), NodeSQRT(NodeMUL(NodeX(), NodeX())), NodeY())),
                NodeIf(NodeBoolean(cond), NodeTriple(NodeADD(NodeX(), NodeY()), NodeADD(NodeX(), NodeY()), NodeADD(NodeX(), NodeY())), NodeTriple(NodeMUL(NodeX(), NodeX()), NodeMUL(NodeY(), NodeY()), NodeMUL(NodeT(), NodeT()))),
                NodeTriple(NodeSUB(NodeX(), NodeY()), NodeMOD(NodeX(), NodeY()), NodeDIV(NodeNumber(0.5f), NodeADD(NodeNumber(r), NodeNumber(0.2f)))));
        }
        static StringBuilder NodeToSb(ref Node node)
        {
            StringBuilder sb = new StringBuilder();
            switch (node.type)
            {
                case NodeType.Number:
                case NodeType.random:
                    sb.Append(node.number);
                    break;
                case NodeType.X:
                    sb.Append("x");
                    break;
                case NodeType.Y:
                    sb.Append("y");
                    break;
                case NodeType.T:
                    sb.Append("t");
                    break;
                case NodeType.SQRT:
                    sb.Append($"{node.type}(");
                    sb.Append(NodeToSb(ref node.unary.expr));
                    sb.Append(")");
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
                    sb.Append(",");
                    sb.Append(NodeToSb(ref node.binary.rhs));
                    sb.Append(")");
                    break;
                case NodeType.If:
                    sb.Append($"if(");
                    sb.Append(NodeToSb(ref node.ternary.first));
                    sb.Append(",");
                    sb.Append(NodeToSb(ref node.ternary.second));
                    sb.Append(",");
                    sb.Append(NodeToSb(ref node.ternary.third));
                    sb.Append(")");
                    break;
                case NodeType.Boolean:
                    sb.Append(node.boolean);
                    break;
                case NodeType.Triple:
                    sb.Append("triple(");
                    sb.Append(NodeToSb(ref node.ternary.first));
                    sb.Append(",");
                    sb.Append(NodeToSb(ref node.ternary.second));
                    sb.Append(",");
                    sb.Append(NodeToSb(ref node.ternary.third));
                    sb.Append(")");
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
        static void NodeSave(string filepath, Node node)
        {
            StringBuilder sb = NodeToSb(ref node);
            File.WriteAllText(filepath, sb.ToString());
        }
        static char? peek(string src, ref int currindex, int offset = 0)
        {
            if (currindex + offset < src.Length)
            {
                return src[currindex + offset];
            }
            return null;
        }
        static char? peek(char type, string src, ref int currindex, int offset = 0)
        {
            char? token = peek(src, ref currindex, offset);
            if (token.HasValue && token.Value == type)
            {
                return token;
            }
            return null;
        }
        static char consume(string src, ref int currindex)
        {
            return src.ElementAt(currindex++);
        }
        static char? tryconsumeerr(char type, string src, ref int currindex)
        {
            if (peek(type, src, ref currindex).HasValue)
            {
                return consume(src, ref currindex);
            }
            Log(LogType.ERROR, $"Error Expected {type}");
            Environment.Exit(1);
            return null;
        }
#pragma warning disable CS8629
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
                case "branch":
                    return NodeType.Branch;
                default:
                    UNREACHABLE("StringToType");
                    return new();
            }
        }
        static Node tokenize(string src, out int currindex)
        {
            currindex = 0;
            StringBuilder buffer = new();
            while (peek(src, ref currindex).HasValue)
            {
                char c = peek(src, ref currindex).Value;
                while (peek(src, ref currindex).HasValue && (char.IsAsciiLetterOrDigit(peek(src, ref currindex).Value) || peek(src, ref currindex).Value == '.' || peek(src, ref currindex).Value == '-'))
                {
                    buffer.Append(consume(src, ref currindex));
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
                        tryconsumeerr('(', src, ref currindex);
                        Node expr = tokenize(src[currindex..], out int step);
                        currindex += step;
                        tryconsumeerr(')', src, ref currindex);
                        Node unary = NodeUnary(expr, type);
                        return unary;
                    case NodeType.ADD:
                    case NodeType.MUL:
                    case NodeType.SUB:
                    case NodeType.GT:
                    case NodeType.GTE:
                    case NodeType.MOD:
                    case NodeType.DIV:
                        tryconsumeerr('(', src, ref currindex);
                        Node lhs = tokenize(src[currindex..], out int lhsstep);
                        currindex += lhsstep;
                        tryconsumeerr(',', src, ref currindex);
                        Node rhs = tokenize(src[currindex..], out int rhsstep);
                        currindex += rhsstep;
                        tryconsumeerr(')', src, ref currindex);
                        Node binary = NodeBinary(lhs, rhs, type);
                        return binary;
                    case NodeType.Triple:
                    case NodeType.If:
                        tryconsumeerr('(', src, ref currindex);
                        Node first = tokenize(src[currindex..], out int firststep);
                        currindex += firststep;
                        tryconsumeerr(',', src, ref currindex);
                        Node second = tokenize(src[currindex..], out int secondstep);
                        currindex += secondstep;
                        tryconsumeerr(',', src, ref currindex);
                        Node third = tokenize(src[currindex..], out int thirdstep);
                        currindex += thirdstep;
                        tryconsumeerr(')', src, ref currindex);
                        Node ternary = NodeTernary(first, second, third, type);
                        return ternary;
                    case NodeType.Branch:
                        throw new NotImplementedException();
                    case NodeType.Number:
                    case NodeType.random:
                    default:
                        UNREACHABLE("tokenize");
                        return new();
                }
            }
            UNREACHABLE("tokenize");
            return new();
        }
#pragma warning restore CS8629
        static Node NodeLoad(string filepath)
        {
            string src = File.ReadAllText(filepath);
            return tokenize(src, out int currindex);
        }
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
                Cli(outputpath, depth);
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
                Gui(depth);
            }
            else
            {
                Usage();
            }
            // TODO:
            //- You need a way to save and load the random function 
            //	- in a format so you can read it later and reuse it in the program
            //	- and same way of grammar handling
            //- You need a way to save and load the grammar
            //  (see if you can modify the code to add the grammar it self and then run it, after that go for the trivial approaches)
            //- A random grammar generator
            //	- you need rules for generation

            //- we may have to redefine the binary operators (add, mul, ...) to take three inputs instead of just (lhs, rhs), and to expand it to a variadic for that matter
            return 0;
        }
    }
}
