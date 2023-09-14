using System;
using CalculatorLib;
internal class Program
{
    private static void Main(string[] args)
    {
        double a, b, res;
        Calculator calc = new Calculator();

        do{//main loop
            do{//continuously grab input until it's valid
                try{
                    Console.WriteLine("Enter a:");
                    a = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    a = double.NaN;
                }
            } while (a == double.NaN);
            
            do{//same for B
                try{
                    Console.WriteLine("Enter b:");
                    b = (double)Convert.ToDouble(Console.ReadLine());
                } catch {
                    Console.Write("Invalid input; ");
                    b = double.NaN;
                }
            } while (b == double.NaN);
            
            do{//same but different for op
                Console.WriteLine("Enter op (+|-|*|/|^|root):");
                res = calc.doOp(a, b, Console.ReadLine() ?? "");
            } while (res == double.NaN);
            
            Console.WriteLine($"Result: {res}");

            Console.WriteLine("Exit? (y|n)");
        } while ((Console.ReadLine() ?? "").Equals("n")); //check for exit
    }
}