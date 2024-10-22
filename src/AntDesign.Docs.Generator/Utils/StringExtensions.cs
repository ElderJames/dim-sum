﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AntDesign.Docs.Generator.Utils
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string str)
        {

            return string.Join("", str.Split('-').Select(static str => str[0].ToString().ToUpper() + str.Substring(1)));
        }

    }
}
