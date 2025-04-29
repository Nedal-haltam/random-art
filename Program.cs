using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable RETURN0001
#pragma warning restore IDE0079 // Remove unnecessary suppression
namespace random_art
{
    public struct Color(byte r, byte g, byte b, byte a = 255)
    {
        public byte r = r;
        public byte g = g;
        public byte b = b;
        public byte a = a;
    }
    public enum NodeType
    {
        number, X, Y, boolean,

        ADD, MUL, SUB, GT, MOD,

        triple,
        If,

    }
    public sealed class NodeBinary(Node lhs, Node rhs)
    {
        public Node lhs = lhs;
        public Node rhs = rhs;
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
        public static void Log(LogType type, string msg)
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
                default: throw new Exception("UNREACHABLE(Log)\n");
            }
            Console.Write(head + msg);
            Console.ForegroundColor = before;
        }



        static Node NodeNumber(float number)
        {
            return new Node() { type = NodeType.number, number = number };
        }
        static Node NodeBoolean(bool boolean)
        {
            return new Node() { type = NodeType.boolean, boolean = boolean };
        }
        static Node NodeX()
        {
            return new Node() { type = NodeType.X };
        }
        static Node NodeY()
        {
            return new Node() { type = NodeType.Y };
        }
        static Node NodeADD(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.ADD, binary = new(lhs, rhs) };
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

        static Node NodeIf(Node cond, Node then, Node elsee)
        {
            return new() { type = NodeType.If, iff = new(cond, then, elsee) };
        }
        static Node NodeTriple(Node first, Node second, Node third)
        {
            return new Node() { type = NodeType.triple, triple = new(first, second, third) };
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
                case NodeType.number:
                case NodeType.boolean:
                case NodeType.X:
                case NodeType.Y:
                case NodeType.If:
                case NodeType.triple:
                default: throw new Exception("UNREACHABLE(EvalBinary)\n");
            }
            ;
#pragma warning restore IDE0066 // Convert switch statement to expression
        }
        static Node? EvalToNode(ref Node f, float x, float y)
        {
            switch (f.type)
            {
                case NodeType.number:
                case NodeType.boolean: return f;
                case NodeType.X: return NodeNumber(x);
                case NodeType.Y: return NodeNumber(y);
                case NodeType.ADD:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
                    Node? lhs = EvalToNode(ref f.binary.lhs, x, y);
                    if (!lhs.HasValue) return null;
                    if (lhs.Value.type != NodeType.number) return null;
                    Node? rhs = EvalToNode(ref f.binary.rhs, x, y);
                    if (!rhs.HasValue) return null;
                    if (rhs.Value.type != NodeType.number) return null;
                    return EvalBinary(lhs.Value, rhs.Value, f.type);
                case NodeType.If:
                    Node? cond = EvalToNode(ref f.iff.cond, x, y);
                    if (!cond.HasValue) return null;
                    if (cond.Value.type != NodeType.boolean) return null;
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
                case NodeType.triple:
                    Node? first = EvalToNode(ref f.triple.first, x, y);
                    if (!first.HasValue) return null;
                    if (first.Value.type != NodeType.number) return null;
                    Node? second = EvalToNode(ref f.triple.second, x, y);
                    if (!second.HasValue) return null;
                    if (second.Value.type != NodeType.number) return null;
                    Node? third = EvalToNode(ref f.triple.third, x, y);
                    if (!third.HasValue) return null;
                    if (third.Value.type != NodeType.number) return null;
                    return NodeTriple(NodeNumber(first.Value.number), NodeNumber(second.Value.number), NodeNumber(third.Value.number));
                default: throw new Exception("UNREACHABLE(EvalToNode)\n");
            }
        }
        static Color? Eval(ref Node f, float x, float y)
        {
            Node? c = EvalToNode(ref f, x, y);

            if (!c.HasValue) return null;
            if (c.Value.type != NodeType.triple) return null;
            if (c.Value.triple.first.type != NodeType.number) return null;
            if (c.Value.triple.second.type != NodeType.number) return null;
            if (c.Value.triple.third.type != NodeType.number) return null;

            return ToColor(new(c.Value.triple.first.number, c.Value.triple.second.number, c.Value.triple.third.number));
        }
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
                    image.Append($"{c.r} {c.g} {c.b}\n");
                }
            }
            File.WriteAllText(FilePath, image.ToString());
            return true;
        }
        static StringBuilder Foo(Node f, int start, int end)
        {
            StringBuilder output = new();
            for (int y = start; y < end; ++y)
            {
                float Normalizedy = ((float)y / HEIGHT) * 2 - 1;
                for (int x = 0; x < WIDTH; ++x)
                {
                    float Normalizedx = ((float)x / WIDTH) * 2 - 1;
                    Color? c = Eval(ref f, Normalizedx, Normalizedy);
                    //if (!c.HasValue)
                    //    return new();
                    output.Append($"{c.Value.r} {c.Value.g} {c.Value.b}\n");
                }
            }
            return output;
        }
        const int taskCount = 10;
        const int WIDTH = 10 * 80;
        const int HEIGHT = 10 * 80;
        static bool GeneratePPM(string FilePath, Node f)
        {
            StringBuilder a = new();
            a.Append($"P3\n{WIDTH} {HEIGHT}\n255\n");
            a = a.Append(Foo(f, 0, HEIGHT));
            File.WriteAllText(FilePath, a.ToString());
            return true;


        }
        static bool TaskGeneratePPM(string FilePath, Node f)
        {
            StringBuilder image = new();
            image.Append($"P3\n{WIDTH} {HEIGHT}\n255\n");
            int portion = HEIGHT / taskCount;

            var tasks = new List<Task<StringBuilder>>();
            Task<StringBuilder> t = new(() => Foo(f, 0, 234));
            for (int i = 0; i < taskCount; ++i)
            {
                tasks.Add(Task.Run(() => Foo(f, i * portion, (i + 1) * portion)));
                //tasks.Add(new(() => Foo(f, i * portion, (i + 1) * portion)));
            }

            Task.WaitAll([.. tasks]);

            for (int i = 0; i < tasks.Count; i++)
            {
                StringBuilder PortionResult = tasks[i].Result;
                image.Append(PortionResult);
            }

            File.WriteAllText(FilePath, image.ToString());
            return true;
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
                case NodeType.number:
                    Console.Write(node.number);
                    break;
                case NodeType.X:
                    Console.Write("x");
                    break;
                case NodeType.Y:
                    Console.Write("y");
                    break;
                case NodeType.ADD:
                case NodeType.SUB:
                case NodeType.MUL:
                case NodeType.MOD:
                case NodeType.GT:
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
                case NodeType.boolean:
                    Console.Write(node.boolean);
                    break;
                case NodeType.triple:
                    NodeTriplePrint(ref node.triple);
                    break;
                default: throw new Exception("UNREACHABLE(NodePrint)");
            }
        }
        static void NodePrintln(ref Node node)
        {
            NodePrint(ref node);
            Console.WriteLine();
        }
        static readonly Random r = new();
        static Node GenNode(int depth)
        {
            if (depth == 0)
            {
                int tt = 1 + r.Next(3);
                if (tt == 1)
                    return NodeX();
                else if (tt == 2)
                    return NodeY();
                else if (tt == 3)
                    return NodeNumber(r.NextSingle() * 2 - 1);
            }
            int t = r.Next(5);
            switch (t)
            {
                case 0:
                case 1: return NodeADD(GenNode(depth - 1), GenNode(depth - 1));
                case 2: return NodeSUB(GenNode(depth - 1), GenNode(depth - 1));
                case 4:
                case 3: return NodeMUL(GenNode(depth - 1), GenNode(depth - 1));
                //case 4: return NodeMOD(GenNode(depth - 1), GenNode(depth - 1));
                default: throw new Exception();
            }
        }
        static Node LoadBasicNode()
        {
            return NodeIf(
                NodeGT(NodeMUL(NodeX(), NodeY()), NodeNumber(0)),
                NodeTriple(NodeX(), NodeY(), NodeNumber(1)),
                NodeTriple(NodeMOD(NodeX(), NodeY()), NodeMOD(NodeX(), NodeY()), NodeMOD(NodeX(), NodeY()))
                );
        }
        static Node LoadFromGrammar(int depth)
        {
            return NodeTriple(GenNode(depth), GenNode(depth), GenNode(depth));
        }
        static int Main(/*string[] args*/)
        {
            // TODO:
            //- You need a way to save and load the grammar (see if you can modify the code to add the grammar it self and then run it, after that go for the trivial approaches)
            //- You need a way to save and load the random function 
            //	- in a format so you can read it later and reuse it in the program
            //	- or same way of grammar handling
            //- A random grammar generator
            //	- you need rules for generation
            //- And for any of that to happen, you need a format (struct/class) for the grammars that is generated or possibly hardcoded
            //- try GPU and shaders to generate the image
            Node f = LoadFromGrammar(5);
            NodePrintln(ref f);
            //Node f = NodeTriple(NodeX(), NodeX(), NodeX());
            Log(LogType.INFO, "Generating the function is done\n");

            Stopwatch stopwatch = Stopwatch.StartNew();
            if (!GeneratePPM("output2.ppm", f))
            {
                Log(LogType.ERROR, "Could not Generate PPM image");
                return 1;
            }
            Log(LogType.INFO, $"PPM image generated\n");
            stopwatch.Stop();
            long ms = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"time taken: {(float)ms / 1000}\n");
            return 0;
        }
    }
}
