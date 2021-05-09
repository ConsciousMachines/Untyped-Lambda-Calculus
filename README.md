# Untyped-Lambda-Calculus
Implemented all the stuff up to page 78. The rest of the book is just parsing the new syntax, the core is the same. 

Chapter 4 was the best explanation of the Y-combinator I have ever read. 

Implementing LC without the formal semantics was interesting as I discovered a bunch of subtle issues. I ended up adding an "evaluate function body" option as it wasn't strictly part of the normal or applicative evaluation. There are no free variables, as per the book, since a variable is always inside a function. But there is a global function table. BTW I added a parser so you can write LC expressions the usual way, too bad it's still too much of a pain to write the type, list, tree stuff from later in the book. 

Here are the semantics I implemented: 
# Substitution:
for x a name, if x =/= y : 
### [subs y]x -> x  
if x = y : 
### [subs y]x -> y    
### [subs y](a b) -> ([subs y]a [subs y]b)
for f a name and e expression, if f = y : 
### [subs y]\f.e -> \f.e     
if f =/= y : 
### [subs y]\f.e -> \f.[subs y]e     


# Evaluation:
if x is a name : 
### x => x 
if x is in the function table : 
### x => fn_table[x] 
### \f.e => \f.e
assuming f has been alpha-converted to avoid free variable capture : 
### (\f.e x) => [subs x]e 
if f is not a function : 
### (f e) => (f eval(e)) 


There is also an optimization. The book mentioned how normal order evaluation repeats the same computation each time since the argument expression is never evaluated. Well, first of all I made all the expressions readonly (immutable) so we can pass one object around wherever copies of it are needed. Additionally I added a tag "is_evaluated" and a pointer to its evaluated counter part. Suddenly after 4 lines of code, you don't need to recompute anything twice! Each lambda expression has a pointer to its evaluated version, and a tag that tells you to look at it rather than compute it again. This made my (((Y add1) ten) ten) cut down from  over 71_000 to 1_383 beta reductions.

```scheme
(((Y add1) ten) ten)

\s.( ( s
       \fst.\snd.snd )
     \s.( ( s
            \fst.\snd.snd )
          \s.( ( s
                 \fst.\snd.snd )
               \s.( ( s
                      \fst.\snd.snd )
                    \s.( ( s
                           \fst.\snd.snd )
                         \s.( ( s
                                \fst.\snd.snd )
                              \s.( ( s
                                     \fst.\snd.snd )
                                   \s.( ( s
                                          \fst.\snd.snd )
                                        \s.( ( s
                                               \fst.\snd.snd )
                                             \s.( ( s
                                                    \fst.\snd.snd )
                                                  \s.( ( s
                                                         \fst.\snd.snd )
                                                       \s.( ( s
                                                              \fst.\snd.snd )
                                                            \s.( ( s
                                                                   \fst.\snd.snd )
                                                                 \s.( ( s
                                                                        \fst.\snd.snd )
                                                                      \s.( ( s
                                                                             \fst.\snd.snd )
                                                                           \s.( ( s
                                                                                  \fst.\snd.snd )
                                                                                \s.( ( s
                                                                                       \fst.\snd.snd )
                                                                                     \s.( ( s
                                                                                            \fst.\snd.snd )
                                                                                          \s.( ( s
                                                                                                 \fst.\snd.snd )
                                                                                               \s.( ( s
                                                                                                      \fst.\snd.snd )
                                                                                                    \x.x ) ) ) ) ) ) ) ) ) ) ) ) ) ) ) ) ) ) ) )
```
