#load "Parsec.fs"
#load "Parser.fs"

open Parsec
open Table.Parser

runString ops () "(2+4)*3"