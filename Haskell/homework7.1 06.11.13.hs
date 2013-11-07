type Parser a = String -> [(a,String)]

empty :: Parser a
empty = \s -> []

sym :: Char -> Parser Char
sym c (t:ts) |c == t = [(c, ts)]
sym _ _              = []

val :: a -> Parser a
val a str = [(a,str)]

infixl 2 |||
(|||) :: Parser a -> Parser a -> Parser a
p1 ||| p2 = \s -> p1 s ++ p2 s

infixl 3 ||>
(||>) :: Parser a -> (a -> Parser b) -> Parser b
p ||> q = \s -> concat [q a s | (a,s) <- p s] 

many :: Parser a -> Parser [a]
many par = par ||> (\a -> many par ||> val . (a:)) ||| val []

opt :: Parser a -> Parser (Maybe a)
opt a = a ||> val . Just ||| val Nothing

eof :: [(a, String)] -> [a]
eof = map fst . filter ((==[]) . snd)

------------------------------------------------------------------------------------------

data E = Var String   
       | Num Integer  
       | Op  String E E

oneOf = foldr ((|||) . sym) empty

letter = oneOf $ '_':(['a'..'z'] ++ ['A'..'Z'])
digit  = oneOf ['0'..'9']

literal = digit  ||> (\a -> many digit ||> \b -> val $ Num $ read (a:b))
ident   = letter ||> (\a -> many (letter ||| digit) ||> \b -> val $ Var (a:b))

primary = ident ||| literal
	              ||| sym '(' ||> (\_ -> expr ||> (\a -> sym ')' ||> \_ -> val a))
	  
multi   = allOptExpr primary multi   ["*","/"]   
addi    = allOptExpr multi   addi    ["+","-"]
reli    = allOptExpr addi    addi    ["<","<=","==","!=",">=",">"]
logiAnd = allOptExpr reli    logiAnd ["&&"]
logiOr  = allOptExpr logiAnd logiOr  ["||"]
	  
allOptExpr :: Parser E -> Parser E -> [String] -> Parser E
allOptExpr par1 par2 lStr = par1 ||> (\a -> op ||> (\o -> par2 ||> \b -> val $ o a b)) ||| par1 where 
  op = foldl (\acc s -> acc ||| prefix s ||> \_ -> val $ Op s) empty lStr where
	  prefix [x]    = sym x
	  prefix (x:xs) = sym x ||> \_ -> prefix xs
 
expr = logiOr

------------------------------------------------------------------------------------------

instance Show E where
  show tree = "\n    " ++ printTree "    " tree ++ "\n" where
    printTree _   (Num a) = show a
    printTree _   (Var s) = s
    printTree str (Op op l r) = "[" ++ op ++ "]--" ++ printTree newStr r ++ "\n" ++ str ++ "|\n" ++ str ++ printTree str l where
      newStr = str ++ "|" ++ map (\_ -> ' ') [1..length op + 3]