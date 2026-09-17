module Assembly 

open Orleans
open global.ShoppingApp.Grains
open System
open global.ShoppingApp.Abstractions

[<assembly: Orleans.ApplicationPartAttribute("ShoppingApp.Grains")>]
()