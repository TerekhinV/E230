using System;
internal class Program
{
    private static void Main(string[] args)
    {
        double? a, b;
        bool loopFlag;

        do{//main loop
            do{//continuously grab input until it's valid
                try{
                    Console.WriteLine("Enter a:");
                    a = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    a = null;
                }
            } while (a is null);
            
            do{//same for B
                try{
                    Console.WriteLine("Enter b:");
                    b = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    b = null;
                }
            } while (b is null);

            do{//same-ish for operator (implemented differently)
                loopFlag = false;
                Console.WriteLine("Enter op (+|-|*|/|^|root):");
                switch (Console.ReadLine() ?? ""){
                    case "+": Console.WriteLine($"Result: {a+b}"); break;
                    case "-": Console.WriteLine($"Result: {a-b}"); break;
                    case "*": Console.WriteLine($"Result: {a*b}"); break;
                    case "/": Console.WriteLine($"Result: {a/b}"); break;
                    case "^": Console.WriteLine($"Result: {Math.Pow((double)a, (double)b)}"); break;
                    case "root": Console.WriteLine($"Result: {Math.Pow((double)a, 1/(double)b)}"); break;
                    default: Console.Write("Invalid operator; "); loopFlag = true; break;
                }
            } while (loopFlag);
            Console.WriteLine("Exit? (y|n)");
        } while ((Console.ReadLine() ?? "").Equals("n")); //check for exit
    }
}