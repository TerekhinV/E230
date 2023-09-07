using System;
internal class Program
{
    private static void Main(string[] args)
    {
        float a, b, c;
        do{
            Console.WriteLine("Enter a:");
            a = (float)Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Enter b:");
            b = (float)Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Enter op (+|-|*|/):");
            string op = Console.ReadLine();
            Console.Write("Result: ");
            switch (op){
                case "+": Console.WriteLine(a+b); break;
                case "-": Console.WriteLine(a-b); break;
                case "*": Console.WriteLine(a*b); break;
                case "/": Console.WriteLine(a/b); break;
                default: Console.WriteLine("Invalid operator"); break;
            }
            Console.WriteLine("Exit? (y|n)");
        } while (Console.ReadLine().Equals("n"));
    }
}