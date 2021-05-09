using System;
using System.Collections.Generic;
using System.Text;
using name = System.String;


namespace lambda_calc
{
    public static class T // tools
    {
        public static int ac = 0; // counter for names (counts how many alpha-conversions we do)
        public static int br = 0; // counter for # of beta-reductions
        public static int os = 0; // counter for # of beta-reductions skipped by optimization
        public static void assert(bool b, string m = "") { if (!b) throw new Exception(m); }
        public static string generate_name(name n) => $"{n}{ac++}";
    }
    public class Parser
    {
        private string fn_name;
        private string fn_body;
        private int i; // positions in the string 
        // Numeric 48 -> 57 Capitals 65 -> 90 Lowers 97 -> 122 Underscore
        public static bool is_alphanumeric(char c) => (!((c < 48) || (c > 57 && c < 65) || (c > 90 && c < 97) || (c > 122)) || (c == '_'));
        // look at the current char
        private char curr_char() => (i < fn_body.Length) ? fn_body[i] : '$';
        // advance the stream by 1 char, and view it 
        private char next_char() { i++; return curr_char(); }
        // eat one char like pacman, then get next_char
        private char consume_char(char c) { T.assert(fn_body[i] == c, $"expected [{c}], got [{fn_body[i]}]"); return next_char(); }
        // parse a variable name which may contain numbers
        private string parse_var_name()
        {
            // align the 2 pointers
            int j = i;
            // move j forward while there are alphanumeric chars
            while ((j < fn_body.Length) && is_alphanumeric(fn_body[j])) j++;
            // between i and j is our alphanumeric identifier
            string ret = fn_body.Substring(i, j - i);
            // advance i, should now be at the first non-alphanumeric char
            i = j;
            return ret;
        }
        // recursive descent parser 
        private expr parse_expr()
        {
            for (char c = curr_char(); c != '$'; c = next_char())
            {
                switch (c)
                {
                    case ('\\'): // FUNC
                        consume_char('\\');
                        string var_name = parse_var_name();
                        consume_char('.');
                        expr body = parse_expr();
                        return new expr(var_name, body);
                    case ('('): // APP 
                        consume_char('(');
                        expr f = parse_expr();
                        consume_char(' ');
                        expr e = parse_expr();
                        consume_char(')');
                        return new expr(f, e);
                    default: return new expr(parse_var_name()); // parsing variables here
                }
            }
            throw new Exception("bug in parse_expr");
        }
        public expr parse(string def, ref Dictionary<string, expr> fn_table)
        {
            i = 0;
            string[] parts = def.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            this.fn_name = parts[0];
            this.fn_body = parts[1];

            expr ret = parse_expr();
            fn_table[fn_name] = ret;
            return ret;
        }
        public void load_fn_table(string file)
        {
            foreach (string l in System.IO.File.ReadAllLines(file, Encoding.ASCII))
            {
                if ((l.StartsWith(';')) || (l.Trim() == "")) continue; // take out comments and empty lines
                expr rez = this.parse(l, ref expr.fn_table);
                //Console.WriteLine($"{l}\n{rez}\n\n");
            }
        }
    }

    public static class Tests
    {
        // higher level constructors for humans
        static public expr name(name n) => new expr(n); // name
        static public expr func(name n, expr e) => new expr(n, e); // function
        static public expr app(expr f, expr e) => new expr(f, e); // application
        public static void Go()
        {
            var identity = func("x", name("x"));
            var make_pair = func("first", func("second", func("func", app(app(name("func"), name("first")), name("second")))));
            var select_first = func("fst", func("snd", name("fst")));
            var select_second = func("fst", func("snd", name("snd")));

            var tru = select_first;
            var fls = select_second;

            var cond = func("a", func("b", func("c", app(app(name("c"), name("a")), name("b")))));
            var not = func("x", app(app(app(cond, fls), tru), name("x")));
            var and = func("x", func("y", app(app(name("x"), name("y")), fls)));
            var or = func("x", func("y", app(app(name("x"), tru), name("y"))));

            var zero = identity;
            var succ = func("n", func("s", app(app(name("s"), fls), name("n")))); // \n.\s.((s false) n)

            var iszero = func("n", app(name("n"), select_first)); // \n.(n \a.\b.a)
            var pred = func("n", app(app(app(iszero, name("n")), zero), app(name("n"), select_second)));

            var one = app(succ, zero);
            var two = app(succ, one);
            var three = app(succ, two);
            var four = app(succ, three);
            var five = app(succ, four);
            var six = app(succ, five);
            var seven = app(succ, six);
            var eight = app(succ, seven);
            var nine = app(succ, eight);
            var ten = app(succ, nine);


            // Y f = \s.(f (s s)) \s.(f (s s))
            // Y = \f.(\s.(f (s s)) \s.(f (s s)))
            var s = func("s", app(name("f"), app(name("s"), name("s"))));
            var Y = func("f", app(s, s));

            // def add1 f x y = if (iszero y) then (x) else f (succ x) (pred y)
            var if_clause = app(iszero, name("y"));
            var then_clause = name("x");
            var _succ_x = app(succ, name("x"));
            var _pred_y = app(pred, name("y"));
            var else_clause = app(app(name("f"), _succ_x), _pred_y);
            var fn_body = app(app(if_clause, then_clause), else_clause);
            var add1 = func("f", func("x", func("y", fn_body)));
            var add = app(Y, add1);






            Parser p = new Parser();
            string _file = @"C:\Users\pwnag\source\repos\lambda_calc\lambda_calc\standard.lambda";
            p.load_fn_table(_file);

            var soy = app(app(add, ten), ten);


            soy.eval();
            expr.fn_table["test"].eval();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Tests.Go();

            Console.ReadKey();
        }
    }
}

/*

(Y id)

(\f.(\s.(f (s s)) \s.(f (s s))) \x.x) 

(\f0.(\s.(f0 (s s)) \s.(f0 (s s))) \x.x) a-conv

(\s.(\x.x (s s)) \s.(\x.x (s s)))  b-reduce

(\s1.(\x.x (s1 s1)) \s.(\x.x (s s)))  a-conv

(\x.x (\s.(\x.x (s s)) \s.(\x.x (s s))))   b-reduce

(\x2.x2 (\s.(\x.x (s s)) \s.(\x.x (s s))))   a-conv

(\s.(\x.x (s s)) \s.(\x.x (s s)))   b-reduce

(\s3.(\x.x (s3 s3)) \s.(\x.x (s s)))   a-conv

(\x.x (\s.(\x.x (s s)) \s.(\x.x (s s))))    b-reduce

repeat 




*/