using System;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Xml;

namespace random_art
{
    public struct Color(byte r, byte g, byte b, byte a = 255)
    {
        public byte r = r;
        public byte g = g;
        public byte b = b;
        public byte a = a;
    }
    public enum NodeBinaryType
    {
        ADD, MUL, SUB
    }
    public sealed class NodeBinary(NodeBinaryType type, Node lhs, Node rhs)
    {
        public NodeBinaryType type = type;
        public Node lhs = lhs;
        public Node rhs = rhs;
    }
    public sealed class NodeTriple(Node first, Node second, Node third)
    {
        public Node first = first;
        public Node second = second;
        public Node third = third;
    }
    public enum NodeType
    {
        Number, X, Y, Binary, Triple
    }
    public struct Node
    {
        public NodeType type;
        public float Number;
        public NodeBinary Binary;
        public NodeTriple Triple;
    }
    internal sealed class Program
    {
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
                default: throw new Exception("UNREACHABLE\n");
            }
            Console.Write(head + msg);
            Console.ForegroundColor = before;
        }
        static Node NodeX()
        {
            return new Node() { type = NodeType.X };
        }
        static Node NodeY()
        {
            return new Node() { type = NodeType.Y };
        }
        static Node NodeNumber(float number)
        {
            return new Node() { type = NodeType.Number, Number = number };
        }
        static Node NodeTriple(Node first, Node second, Node third)
        {
            return new Node() { type = NodeType.Triple, Triple = new(first, second, third) };
        }
        static Node NodeADD(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.Binary, Binary = new(NodeBinaryType.ADD, lhs, rhs) };
        }
        static Node NodeSUB(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.Binary, Binary = new(NodeBinaryType.SUB, lhs, rhs) };
        }
        static Node NodeMUL(Node lhs, Node rhs)
        {
            return new Node() { type = NodeType.Binary, Binary = new(NodeBinaryType.MUL, lhs, rhs) };
        }
        const int WIDTH = 256;
        const int HEIGHT = 256;
        static Color ToColor(Vector3 v, float min = -1, float max = 1)
        {
            // min..max
            // 0..255
            float m = -min;
            return new((byte)((v.X + (m)) * (255.0f / (max - min))), (byte)((v.Y + (m)) * (255.0f / (max - min))), (byte)((v.Z + (m)) * (255.0f / (max - min))));
        }
        static Node EvalBinary(Node lhs, Node rhs, NodeBinaryType type)
        {
            return type switch
            {
                NodeBinaryType.ADD => NodeNumber(lhs.Number + rhs.Number),
                NodeBinaryType.SUB => NodeNumber(lhs.Number - rhs.Number),
                NodeBinaryType.MUL => NodeNumber(lhs.Number * rhs.Number),
                _ => throw new Exception("UNREACHABLE(evalBinary)\n"),
            };
        }
        static Node? EvalToNode(ref Node f, float x, float y)
        {
            switch (f.type)
            {
                case NodeType.Number: return f;
                case NodeType.X: return NodeNumber(x);
                case NodeType.Y: return NodeNumber(y);
                case NodeType.Binary:
                    Node? lhs = EvalToNode(ref f.Binary.lhs, x, y);
                    if (!lhs.HasValue) return null;
                    if (lhs.Value.type != NodeType.Number) return null;
                    Node? rhs = EvalToNode(ref f.Binary.rhs, x, y);
                    if (!rhs.HasValue) return null;
                    if (rhs.Value.type != NodeType.Number) return null;
                    return EvalBinary(lhs.Value, rhs.Value, f.Binary.type);
                case NodeType.Triple:
                    Node? first = EvalToNode(ref f.Triple.first, x, y);
                    if (!first.HasValue) return null;
                    if (first.Value.type != NodeType.Number) return null;
                    Node? second = EvalToNode(ref f.Triple.second, x, y);
                    if (!second.HasValue) return null;
                    if (second.Value.type != NodeType.Number) return null;
                    Node? third = EvalToNode(ref f.Triple.third, x, y);
                    if (!third.HasValue) return null;
                    if (third.Value.type != NodeType.Number) return null;
                    return NodeTriple(NodeNumber(first.Value.Number), NodeNumber(second.Value.Number), NodeNumber(third.Value.Number));
                default: throw new Exception("UNREACHABLE(eval)\n");
            }
        }
        static Color? eval(ref Node f, float x, float y)
        {
            Node? c = EvalToNode(ref f, x, y);

            if (!c.HasValue) return null;
            if (c.Value.type != NodeType.Triple) return null;
            if (c.Value.Triple.first.type != NodeType.Number) return null;
            if (c.Value.Triple.second.type != NodeType.Number) return null;
            if (c.Value.Triple.third.type != NodeType.Number) return null;

            return ToColor(new(c.Value.Triple.first.Number, c.Value.Triple.second.Number, c.Value.Triple.third.Number));
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
        static bool GeneratePPM(string FilePath, ref Node f)
        {
            StringBuilder image = new();
            image.Append($"P3\n{WIDTH} {HEIGHT}\n255\n");
            for (int y = 0; y < HEIGHT; ++y)
            {
                float Normalizedy = ((float)y / HEIGHT) * 2 - 1;
                for (int x = 0; x < WIDTH; ++x)
                {
                    float Normalizedx = ((float)x / WIDTH) * 2 - 1;
                    Color? c = eval(ref f, Normalizedx, Normalizedy);
                    if (!c.HasValue)
                        return false;
                    image.Append($"{c.Value.r} {c.Value.g} {c.Value.b}\n");
                }
            }
            File.WriteAllText(FilePath, image.ToString());
            return true;
        }
        static void NodeBinaryPrint(NodeBinary binary)
        {
            Console.Write("triple(");
            NodePrint(ref binary.lhs);
            Console.Write(", ");
            NodePrint(ref binary.rhs);
            Console.Write(")");
        }
        static void NodeTriplePrint(NodeTriple triple)
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
                    Console.Write(node.Number);
                    break;
                case NodeType.X:
                    Console.Write("x");
                    break;
                case NodeType.Y:
                    Console.Write("y");
                    break;
                case NodeType.Binary:
                    NodeBinaryPrint(node.Binary);
                    break;
                case NodeType.Triple:
                    NodeTriplePrint(node.Triple);
                    break;
                default: throw new Exception("UNREACHABLE(NodePrint)");
            }
        }
        static Color GenColorFromCoord(float x, float y)
        {
            Vector3 v = new(x, y, x - y);
            return ToColor(v);
            //if (x * y >= 0) return ToColor(new(x, y, 1));
            //float t = x % y;
            //return ToColor(new(t, t, t));
        }
        static int Main(string[] args)
        {
            string FilePath = "output.ppm";

            if (!GeneratePPM("output.ppm", GenColorFromCoord))
            {
                Log(LogType.ERROR, "Could not Generate PPM image");
                return 1;
            }

            Node f = NodeTriple(NodeX(), NodeY(), NodeSUB(NodeX(), NodeY()));
            //Node f = NodeX();
            if (!GeneratePPM("output2.ppm", ref f))
            {
                Log(LogType.ERROR, "Could not Generate PPM image");
                return 1;
            }

            Log(LogType.INFO, $"PPM image generated: {FilePath}\n");
            return 0;
        }
    }
}
