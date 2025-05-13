using System.Numerics;
using System.Text;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using static random_art.Program;



#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable RETURN0001
#pragma warning restore IDE0079 // Remove unnecessary suppression
namespace random_art
{
    public enum NodeType
    {
        Number, random, X, Y, Z, T, Boolean,
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

    public sealed class NodeMonoid
    {
        public NodeMonoid(){}
        public NodeMonoid(List<Node> nodes, float Identity)
        {
            this.nodes = nodes;
            this.Identity = Identity;
        }
        public List<Node> nodes = [];
        public float Identity;
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
        public NodeMonoid binary;
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
        static Node NodeBranch(int branch) => new() { type = NodeType.Branch, branch = branch };
        static Node NodeNumber(float number) => new() { type = NodeType.Number, number = number };
        static Node NodeBoolean(bool boolean) => new() { type = NodeType.Boolean, boolean = boolean };
        static Node NodeRandom() => new() { type = NodeType.random, number = random.NextSingle() * 2 - 1 };
        static Node NodeX() => new() { type = NodeType.X };
        static Node NodeY() => new() { type = NodeType.Y };
        static Node NodeZ() => new() { type = NodeType.Z };
        static Node NodeT() => new() { type = NodeType.T };
        static Node NodeSQRT(Node expr) => NodeUnary(expr, NodeType.SQRT);
        static Node NodeADD(List<Node> nodes) => NodeMonoid(nodes, NodeType.ADD, 0);
        static Node NodeSUB(List<Node> nodes) => NodeMonoid(nodes, NodeType.SUB, 0);
        static Node NodeDIV(List<Node> nodes) => NodeMonoid(nodes, NodeType.DIV, 1);
        static Node NodeMUL(List<Node> nodes) => NodeMonoid(nodes, NodeType.MUL, 1);
        static Node NodeMOD(List<Node> nodes) => NodeMonoid(nodes, NodeType.MOD, 1);
        static Node NodeGT(List<Node> nodes) => NodeMonoid(nodes, NodeType.GT, float.MinValue);
        static Node NodeGTE(List<Node> nodes) => NodeMonoid(nodes, NodeType.GTE, float.MinValue);
        static Node NodeIf(Node cond, Node then, Node elsee) => NodeTernary(cond, then, elsee, NodeType.If);
        static Node NodeTriple(Node first, Node second, Node third) => NodeTernary(first, second, third, NodeType.Triple);
        static Node NodeUnary(Node expr, NodeType type) => new() { type = type, unary = new(expr) };
        static float GetIdentity(NodeType type)
        {
            float iden = (type == NodeType.ADD || type == NodeType.SUB) ? 0 : 1;
            iden = (type == NodeType.GT || type == NodeType.GTE) ? float.MinValue : iden;
            return iden;
        }
        static Node NodeMonoid(List<Node> nodes, NodeType type, float? Identity = null)
        {
            if (!Identity.HasValue)
            {
                float iden = GetIdentity(type);
                return new() { type = type, binary = new(nodes, iden) };
            }
            else
                return new() { type = type, binary = new(nodes, Identity.Value) };
        }
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
                        new(NodeSQRT(
                            NodeADD([NodeMUL([NodeX(), NodeX()]), NodeMUL([NodeY(), NodeY()]), NodeMUL([NodeT(), NodeT()])])), 1),
                    ] },
                    new() { nodes = [new(NodeBranch(1), 1), new(NodeADD([NodeBranch(2), NodeBranch(2)]), 1), new(NodeMUL([NodeBranch(2), NodeBranch(2)]), 1)] },
                ],
                startbranchindex = 0,
                terminalbranchindex = [1],
            };
        }
        static Node GrammarToNode(Grammar grammar, Node node, int Depth)
        {
            if (Depth <= 0)
            {
                Branch b = grammar.branches[grammar.terminalbranchindex[random.Next(grammar.terminalbranchindex.Count)]];
                return BranchToNode(grammar, b.nodes[random.Next(b.nodes.Count)].node, 0);
            }
            else
            {
                Branch b = grammar.branches[node.branch];
                return BranchToNode(grammar, b.nodes[random.Next(b.nodes.Count)].node, Depth - 1);
            }
        }
        static Node BranchToNode(Grammar grammar, Node node, int Depth)
        {
            switch (node.type)
            {
                case NodeType.Branch:
                    return GrammarToNode(grammar, node, Depth - 1);
                case NodeType.random:
                    return NodeNumber(node.number);
                case NodeType.Number:
                case NodeType.X:
                case NodeType.Y:
                case NodeType.Z:
                case NodeType.Boolean:
                case NodeType.T:
                    return node;
                case NodeType.SQRT:
                    return NodeUnary(BranchToNode(grammar, node.unary.expr, Depth), node.type);
                case NodeType.ADD:
                case NodeType.MUL:
                case NodeType.SUB:
                case NodeType.GT:
                case NodeType.GTE:
                case NodeType.MOD:
                case NodeType.DIV:
                    List<Node> nodes = [];
                    foreach(Node n in node.binary.nodes)
                        nodes.Add(BranchToNode(grammar, n, Depth));
                    return NodeMonoid(nodes, node.type);
                case NodeType.Triple:
                case NodeType.If:
                    return NodeTernary(BranchToNode(grammar, node.ternary.first, Depth), BranchToNode(grammar, node.ternary.second, Depth), BranchToNode(grammar, node.ternary.third, Depth), node.type);
                default:
                    Shartilities.UNREACHABLE("BranchToNode");
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
                case NodeType.Z:
                    return new("(z)");
                case NodeType.T:
                    return new("(t)");
                case NodeType.Boolean:
                    return new((f.boolean) ? "(true)" : "(false)");
                case NodeType.ADD:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" + {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.MUL:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" * {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.SUB:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" - {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.GT:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" > {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.GTE:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" >= {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.MOD:
                    {
                        StringBuilder sb = new();
                        sb.Append($"mod({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" mod({NodeToShaderFunction(n)}");
                        for (int i = 0; i < f.binary.nodes.Count; i++)
                            sb.Append(')');
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.DIV:
                    {
                        StringBuilder sb = new();
                        sb.Append($"({GetIdentity(f.type)}");
                        foreach (Node n in f.binary.nodes)
                            sb.Append($" / {NodeToShaderFunction(n)}");
                        sb.Append(")");
                        return sb;
                    }
                case NodeType.SQRT:
                    return new($"(sqrt({NodeToShaderFunction(f.unary.expr)}))");
                case NodeType.Triple:
                    return new($"(vec3({NodeToShaderFunction(f.ternary.first)}, {NodeToShaderFunction(f.ternary.second)}, {NodeToShaderFunction(f.ternary.third)}))");
                case NodeType.If:
                    return new($"(({NodeToShaderFunction(f.ternary.first)}) ? ({NodeToShaderFunction(f.ternary.second)}) : ({NodeToShaderFunction(f.ternary.third)}))");
                case NodeType.Branch:
                default:
                    Shartilities.UNREACHABLE("NodeToShaderFunction");
                    return new();
            }
        }
        static StringBuilder NodeToShader(Node f)
        {
            StringBuilder FragmentShader = new();
            string ShaderFunction = NodeToShaderFunction(f).ToString();
            FragmentShader.Append("#version 330\n");
            FragmentShader.Append("in vec2 fragTexCoord;\n");
            FragmentShader.Append("in vec4 gl_FragCoord;\n");
            FragmentShader.Append("out vec4 finalColor;\n");
            FragmentShader.Append("uniform float csTIME;\n");
            FragmentShader.Append("void main()\n");
            FragmentShader.Append("{\n");
            FragmentShader.Append("float x = 2.0 * (fragTexCoord.x) - 1.0;\n");
            FragmentShader.Append("float y = 2.0 * (fragTexCoord.y) - 1.0;\n");
            FragmentShader.Append("float t = sin(csTIME);\n");
            FragmentShader.Append($"   vec3 tempcolor = {ShaderFunction};\n");
            FragmentShader.Append("    finalColor = vec4((tempcolor + 1) / 2.0, 1);\n");
            FragmentShader.Append('}');
            return FragmentShader;
        }
        static (Node, StringBuilder) GrammarToShader(Grammar g, int Depth)
        {
            Node n = GrammarToNode(g, NodeBranch(g.startbranchindex), Depth);
            return (n, NodeToShader(n));
        }
        public static Texture2D LoadDefaultTexture() => new() { Id = 1, Width = 1, Height = 1, Mipmaps = 1, Format = PixelFormat.UncompressedR8G8B8A8 };

        [RequiresUnreferencedCode("Calls random_art.Program.LoadOject<T>(String)")]
        static void Gui(int Width, int Height, int Depth)
        {
            Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(Width, Height, "Random Art");
            Raylib.SetTargetFPS(60);
            Texture2D DefaultTexture = LoadDefaultTexture();
            Grammar g = LoadDefaultGrammar();
            float csTime = 0;
            (Node node, StringBuilder FragmentShader) = GrammarToShader(g, Depth);
            Shader Shader = Raylib.LoadShaderFromMemory(null, FragmentShader.ToString());
            Node currentnode = node;
            while (!Raylib.WindowShouldClose())
            {
                Width = Raylib.GetScreenWidth();
                Height = Raylib.GetScreenHeight();
                csTime += Raylib.GetFrameTime();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);
                Raylib.SetShaderValue(Shader, Raylib.GetShaderLocation(Shader, "csTIME"), csTime, ShaderUniformDataType.Float);
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    csTime = 0;
                    (Node tempnode, StringBuilder tempFragmentShader) = GrammarToShader(g, Depth);
                    currentnode = tempnode;
                    Raylib.UnloadShader(Shader);
                    Shader = Raylib.LoadShaderFromMemory(null, tempFragmentShader.ToString());
                }
                if (Raylib.IsKeyPressed(KeyboardKey.S))
                {
                    string FilePath = "Node.txt";
                    if (!NodeSave(FilePath, currentnode))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"could not save node because of : {ERROR_MESSAGE}\n");
                    else
                        Shartilities.Log(Shartilities.LogType.INFO, $"node saved successfully into : {FilePath}\n");
                }
                if (Raylib.IsKeyPressed(KeyboardKey.L))
                {
                    csTime = 0;
                    string FilePath = "Node.txt";
                    Node? tempnode = NodeLoad(FilePath);
                    if (!tempnode.HasValue)
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"could not load node because of : {ERROR_MESSAGE}\n");
                    }
                    else
                    {
                        Shartilities.Log(Shartilities.LogType.INFO, $"node loaded successfully from : {FilePath}\n");
                        Shartilities.Log(Shartilities.LogType.NORMAL, "compiling node into a fragment shader\n");
                        StringBuilder tempFragmentShader = NodeToShader(tempnode.Value);
                        currentnode = tempnode.Value;
                        Raylib.UnloadShader(Shader);
                        Shader = Raylib.LoadShaderFromMemory(null, tempFragmentShader.ToString());
                    }
                }
                Raylib.BeginShaderMode(Shader);
                Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, Width, Height), new(0, 0), 0, Color.White);
                Raylib.EndShaderMode();

                Raylib.DrawFPS(0, 0);
                Raylib.EndDrawing();
            }
            Raylib.UnloadShader(Shader);
            Raylib.CloseWindow();
        }
        static bool GrammarToImage(Grammar g, int Width, int Height, string FilePath, int Depth)
        {
            (Node _, StringBuilder shader) = GrammarToShader(g, Depth);
            Texture2D DefaultTexture = LoadDefaultTexture();
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(Width, Height, "");
            Shader s = Raylib.LoadShaderFromMemory(null, shader.ToString());
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginShaderMode(s);
            Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, Width, Height), new(0, 0), 0, Color.White);
            Raylib.EndShaderMode();
            Image image = Raylib.LoadImageFromScreen();
            if (!Raylib.ExportImage(image, FilePath))
            {
                ERROR_MESSAGE = $"Failed to export image to path: {FilePath}\n";
                return false;
            }
            Raylib.UnloadShader(s);
            Raylib.UnloadImage(image);
            Raylib.CloseWindow();
            return true;
        }
        static bool NodeToImage(Node node, int Width, int Height, string FilePath)
        {
            Texture2D DefaultTexture = LoadDefaultTexture();
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(Width, Height, "");
            Shader s = Raylib.LoadShaderFromMemory(null, NodeToShader(node).ToString());
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginShaderMode(s);
            Raylib.DrawTexturePro(DefaultTexture, new(0, 0, DefaultTexture.Width, DefaultTexture.Height), new(0, 0, Width, Height), new(0, 0), 0, Color.White);
            Raylib.EndShaderMode();
            Image image = Raylib.LoadImageFromScreen();
            if (!Raylib.ExportImage(image, FilePath))
            {
                ERROR_MESSAGE = $"Failed to export image to path: {FilePath}\n";
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
            Shartilities.Log(Shartilities.LogType.NORMAL, "\nUsage: \n");
            Shartilities.Log(Shartilities.LogType.NORMAL, $".\\random-art.exe [gui|cli] [option(s)]\n");
            Shartilities.Log(Shartilities.LogType.NORMAL, "\nOptions:\n");
            Shartilities.Log(Shartilities.LogType.NORMAL, $"\t{"-o <file>",-15} : place the output image into <file>\n");
            Shartilities.Log(Shartilities.LogType.NORMAL, $"\t{"-depth <depth>",-15} : specify the depth of the generated function\n\n");
        }
        public struct Camera
        {
            public Camera3D Camera3D;
            public float CameraSpeed;
            public float ZoomSpeed;
            public float RotationX;
            public float RotationY;
            public float Sensitivity;
            public void Update()
            {
                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    Vector2 md = Raylib.GetMouseDelta();
                    this.RotationX -= md.Y * this.Sensitivity;
                    this.RotationY += md.X * this.Sensitivity;
                }
                this.Camera3D.Position.Z += this.ZoomSpeed * Raylib.GetMouseWheelMove() * Raylib.GetFrameTime();
            }
            public readonly void UpdateRotaions()
            {
                Rlgl.Translatef(0.0f, 0.0f, 0.0f);
                Rlgl.Rotatef(this.RotationX, 1.0f, 0.0f, 0.0f);
                Rlgl.Rotatef(this.RotationY, 0.0f, 1.0f, 0.0f);
            }
        }
        public struct Pixel(Vector3 p, Color c)
        {
            public Vector3 Position = p;
            public Color Color = c;
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
                case NodeType.Z:
                    sb.Append('z');
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
                    sb.Append('0');
                    for (int i = 0; i < node.binary.nodes.Count; i++)
                    {
                        sb.Append(',');
                        Node n = node.binary.nodes[i];
                        sb.Append(NodeToSb(ref n));
                    }
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
                    Shartilities.UNREACHABLE("NodeToSb");
                    return new();
            }
            return sb;
        }
        static char? Peek(string src, ref int index, int offset = 0)
        {
            if (index+ offset < src.Length)
            {
                return src[index+ offset];
            }
            return null;
        }
        static char? Peek(char type, string src, ref int index, int offset = 0)
        {
            char? token = Peek(src, ref index, offset);
            if (token.HasValue && token.Value == type)
            {
                return token;
            }
            return null;
        }
        static char Consume(string src, ref int index)
        {
            return src.ElementAt(index++);
        }
        static char? Tryconsumeerr(char type, string src, ref int index)
        {
            if (Peek(type, src, ref index).HasValue)
            {
                return Consume(src, ref index);
            }
            Shartilities.Log(Shartilities.LogType.ERROR, $"Error Expected {type}\n");
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
                    Shartilities.UNREACHABLE("StringToType");
                    return new();
                default:
                    return NodeType.Branch;
            }
        }
#pragma warning disable CS8629
        static Node TokenizeNode(string src, out int index)
        {
            // we may not have to take a substring whenever we want to tokenize, we can make it global and pass only the index, and that would be suffecient enough
            index= 0;
            StringBuilder Buffer = new();
            while (Peek(src, ref index).HasValue)
            {
                while (Peek(src, ref index).HasValue && (char.IsAsciiLetterOrDigit(Peek(src, ref index).Value) || Peek(src, ref index).Value == '.' || Peek(src, ref index).Value == '-' || char.IsWhiteSpace(Peek(src, ref index).Value)))
                {
                    if (char.IsWhiteSpace(Peek(src, ref index).Value))
                        Consume(src, ref index);
                    else
                        Buffer.Append(Consume(src, ref index));
                }
                string token = Buffer.ToString().ToLower();
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
                    case NodeType.Z:
                        return NodeZ();
                    case NodeType.T:
                        return NodeT();
                    case NodeType.Boolean:
                        return NodeBoolean(token == "true");
                    case NodeType.SQRT:
                        Tryconsumeerr('(', src, ref index);
                        Node expr = TokenizeNode(src[index..], out int exprstep);
                        index+= exprstep;
                        Tryconsumeerr(')', src, ref index);
                        Node unary = NodeUnary(expr, type);
                        return unary;
                    case NodeType.ADD:
                    case NodeType.MUL:
                    case NodeType.SUB:
                    case NodeType.GT:
                    case NodeType.GTE:
                    case NodeType.MOD:
                    case NodeType.DIV:
                        Shartilities.UNREACHABLE("TODO: binary tokenization");
                        return new();
                        //Tryconsumeerr('(', src, ref index);
                        //Node lhs = TokenizeNode(src[index..], out int lhsstep);
                        //index+= lhsstep;
                        //Tryconsumeerr(',', src, ref index);
                        //Node rhs = TokenizeNode(src[index..], out int rhsstep);
                        //index+= rhsstep;
                        //Tryconsumeerr(')', src, ref index);
                        //Node binary = NodeMonoid(lhs, rhs, type);
                        //return binary;
                    case NodeType.Triple:
                    case NodeType.If:
                        Tryconsumeerr('(', src, ref index);
                        Node first = TokenizeNode(src[index..], out int firststep);
                        index+= firststep;
                        Tryconsumeerr(',', src, ref index);
                        Node second = TokenizeNode(src[index..], out int secondstep);
                        index+= secondstep;
                        Tryconsumeerr(',', src, ref index);
                        Node third = TokenizeNode(src[index..], out int thirdstep);
                        index+= thirdstep;
                        Tryconsumeerr(')', src, ref index);
                        Node ternary = NodeTernary(first, second, third, type);
                        return ternary;
                    case NodeType.Branch:
                        Shartilities.UNREACHABLE("TokenizeNode");
                        return new();
                    case NodeType.Number:
                    case NodeType.random:
                    default:
                        Shartilities.UNREACHABLE("TokenizeNode");
                        return new();
                }
            }
            Shartilities.UNREACHABLE("TokenizeNode");
            return new();
        }
#pragma warning restore CS8629
        static bool NodeSave(string FilePath, Node node)
        {
            StringBuilder sb = NodeToSb(ref node);
            try
            {
                File.WriteAllText(FilePath, sb.ToString());
            }
            catch (Exception e)
            {
                ERROR_MESSAGE = e.Message;
                return false;
            }
            return true;
        }
        static Node? NodeLoad(string FilePath)
        {
            string src;
            try
            {
                src = File.ReadAllText(FilePath);
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
            string Mode = Shartilities.ShifArgs(ref args, "UNREACHABLE");

            if (Mode.Equals("-h", StringComparison.CurrentCultureIgnoreCase)) { Usage(); Environment.Exit(0); }

            if (Mode.Equals("cli", StringComparison.CurrentCultureIgnoreCase))
            {
                string OutputPath = "Default_output_path.png";
                int Depth = 20;
                while (args.Length > 0)
                {
                    string Flag = Shartilities.ShifArgs(ref args, "");
                    if (Flag == "-o")
                    {
                        OutputPath = Shartilities.ShifArgs(ref args, "No output file path provided\n");
                    }
                    else if (Flag == "-depth")
                    {
                        string DepthFlag = Shartilities.ShifArgs(ref args, "No depth provided\n");
                        if (!int.TryParse(DepthFlag, out Depth))
                            Shartilities.Log(Shartilities.LogType.ERROR, "Could not parse depth\n");
                    }
                    else
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, "invalid flag\n");
                    }
                }
                if (!GrammarToImage(LoadDefaultGrammar(), 800, 800, OutputPath, Depth))
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, ERROR_MESSAGE);
                }
            }
            else if (Mode.Equals("gui", StringComparison.CurrentCultureIgnoreCase))
            {
                int Depth = 20;
                while (args.Length > 0)
                {
                    string Flag = Shartilities.ShifArgs(ref args, "");
                    if (Flag == "-depth")
                    {
                        string DepthFlag = Shartilities.ShifArgs(ref args, "No depth provided\n");
                        if (!int.TryParse(DepthFlag, out Depth))
                            Shartilities.Log(Shartilities.LogType.ERROR, "Could not parse depth\n");
                    }
                    else
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, "invalid flag\n");
                    }
                }
                Gui(800, 800, Depth);
            }
            else
            {
                Usage();
            }
            // TODO:
            //  - add more flags (Width, Height, generate videos using ffmpeg pipeline)
            //  - A random grammar generator
            //  	- you need rules for generation
            //  - test all monoid operations as separate nodes
            return 0;
        }
    }
}