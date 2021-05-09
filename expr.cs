using System;
using System.Text;
using System.Collections.Generic;
using name = System.String;


namespace lambda_calc
{
    public class expr
    {
        static public Dictionary<string, expr> fn_table = new Dictionary<name, expr>(); // function table
        // house keeping
        private string print(int depth = 0, bool newline = true)
        {
            StringBuilder ret = new StringBuilder();
            if (newline) ret.Append('\n' + new String(' ', depth));
            switch (this.t)
            {
                case (type.NAME): ret.Append(this.n); break;
                case (type.FUNC):
                    {
                        var intro = $"\\{this.n}.";
                        depth += intro.Length;
                        ret.Append(intro);
                        ret.Append(this.e.print(depth, false));
                        break;
                    }
                case (type.APP):
                    {
                        ret.Append("( ");
                        depth += 2;
                        ret.Append(this.f.print(depth, false));
                        ret.Append(this.e.print(depth));
                        ret.Append(" )");
                        break;
                    }
            }
            return ret.ToString();
        }
        public override string ToString() => this.print(0);
        private void reset_is_evaluated() // recursively sets is_evaluated to false (so we can re-evaluate it w different method)
        {
            this.is_evaluated = false;
            switch (this.t)
            {
                case (type.NAME): return; // names are always already evaluated
                case (type.FUNC): this.e.reset_is_evaluated(); return;
                case (type.APP): this.f.reset_is_evaluated(); this.e.reset_is_evaluated(); return;
            }
        }



        // optimization
        public bool is_evaluated = false;
        public expr evaluated_version;


        private enum type { NAME, FUNC, APP };
        //     n -> n just a variable/name/string/symbol
        //     \n.e -> n is a bound variable, body is a lambda-expression
        //     (f e) -> f is the function, e is the argument lambda-expression
        readonly private type t;
        readonly private name n; // used as name for type.NAME, and bound variable for type.FUNC
        readonly private expr f; // only used as (f e) in type.APP
        readonly private expr e; // used as \n.e in type.FUNC, or (f e) in type.APP


        // constructors 
        public expr(name n) { this.t = type.NAME; this.n = n; } // name
        public expr(name n, expr e) { this.t = type.FUNC; this.n = n; this.e = e; } // function
        public expr(expr f, expr e) { this.t = type.APP; this.f = f; this.e = e; } // application

        public expr eval(bool normal = true, bool eval_fn_body = false) // high-level wrapper for evaluate 
        {
            T.ac = T.br = T.os = 0; // reset counters
            expr ret = this.evaluate(normal, eval_fn_body); // eval
            Console.WriteLine($"\n[ F I N A L   O U T P U T ] :: used {T.br} beta-reductions, {T.ac} alpha-conversions, {T.os} optimization-skips");
            Console.WriteLine(ret);

            // have to reset table bc durig eval, some expr's pointed to its functions (and 'evaluated' under normal is diff from 'evaluated' under applicative)
            foreach (name key in fn_table.Keys) fn_table[key].reset_is_evaluated();
            T.ac = T.br = T.os = 0; // reset counters
            ret.reset_is_evaluated();
            expr reta = ret.evaluate(false, true); // applicative order reduce the terminated result, and eval fn bodies
            Console.WriteLine($"\n[ Applicative Version ] :: used {T.br} beta-reductions, {T.ac} alpha-conversions, {T.os} optimization-skips");
            Console.WriteLine(reta);

            return ret;
        }


        private expr subs(name nam, expr e_new) // only called by itself, and in eval during beta-reduction and a-conversion
        {
            // WHEN BETA-REDUCING, WE ARE ALREADY IN THE BODY OF THE FUNCTION, SO THE "BOUNDED" VAR IS "FREE" HERE. 
            switch (this.t)
            {
                // expr is a name. If names match, return the substitute, else the original name. 
                case (type.NAME): return this.n == nam ? e_new : this;

                // APP: substitute both f and e 
                case (type.APP): return new expr(this.f.subs(nam, e_new), this.e.subs(nam, e_new));

                // FUNC: \f.\f.(...) -> return \f.(...), aka nothing got alpha-converted. otherwise subs body
                case (type.FUNC): return this.n == nam ? this : new expr(this.n, this.e.subs(nam, e_new));
            }
            throw new Exception("bug in subs");
        }

        public expr evaluate(bool normal, bool eval_fn_body)
        {
            //if (this.is_evaluated) { T.os++; return this.evaluated_version; } // optimization
            T.br++;
            //Console.WriteLine($"\n\n{this.t}\n{this}");

            switch (this.t)
            {
                case (type.NAME): // replace names by user defined functions if they are in the fn_table
                    {
                        this.evaluated_version = expr.fn_table.ContainsKey(this.n) ? expr.fn_table[this.n].evaluate(normal, eval_fn_body) : this;
                        break;
                    }
                case (type.FUNC): // TAPL says we don't eval inside fn body, even under applicative mode; so i added special option for it
                    {
                        this.evaluated_version = eval_fn_body ? new expr(this.n, this.e.evaluate(normal, eval_fn_body)) : this;
                        break;
                    }
                case (type.APP):
                    {
                        // evaluate f. 
                        expr f_ev = this.f.evaluate(normal, eval_fn_body);

                        if (f_ev.t == type.FUNC) // f is FUNC, BETA-REDUCE
                        {
                            // alpha-convert in prep for beta-reduction, since we don't use deBruijn
                            name new_name = T.generate_name(this.n);

                            // in the case of \f.(\f.f \f.f) substitution will stop upon entering nested fn with same bound variable
                            expr f_ev_ac = f_ev.subs(this.n, new expr(new_name));

                            // evaluate argument if we are not in normal mode 
                            expr arg = normal ? this.e : this.e.evaluate(normal, eval_fn_body);

                            // we a-converted f_eval, so whatever is in arg won't name clash with it. 
                            this.evaluated_version = f_ev_ac.e.subs(f_ev_ac.n, arg).evaluate(normal, eval_fn_body);
                        }
                        // if f is not a function, return (f <whatever>) and eval the RHS (couldn't reduce the APP)
                        else this.evaluated_version = new expr(f_ev, this.e.evaluate(normal, eval_fn_body));

                        break;
                    }
            }
            this.is_evaluated = true; // the Lambda-Expression now has an evaluated version attached to it
            return this.evaluated_version;
        }
    }
}
