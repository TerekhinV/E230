using System;
using CalculatorLib;
internal class Program
{
    private static void Main(string[] args)
    {
        double a, b, res;
        Calculator calc = new Calculator();

        Console.WriteLine("C# console calculator\n");
        do{//main loop
            do{//continuously grab input until it's valid
                try{
                    Console.WriteLine("Enter a:");
                    a = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    a = double.NaN;
                }
            } while (Double.IsNaN(a));
            
            do{//same for B
                try{
                    Console.WriteLine("Enter b:");
                    b = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    b = double.NaN;
                }
            } while (Double.IsNaN(b));
            
            while(true){//same but different for op
                Console.WriteLine("Enter op (+|-|*|/|^|root):");
                res = calc.doOp(a, b, Console.ReadLine() ?? "");
                if (!Double.IsNaN(res)) break;
                Console.Write("Invalid input; ");
            }
            
            Console.WriteLine($"Result: {res}");

            Console.WriteLine("Exit? (y|n)");
        } while ((Console.ReadLine() ?? "").Equals("n")); //check for exit
        calc.Finish();
    }
}