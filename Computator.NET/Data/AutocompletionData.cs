﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using AutocompleteMenuNS;
using Computator.NET.Compilation;
using Computator.NET.Constants;
using Computator.NET.Functions;
using Computator.NET.Transformations;
using Computator.NET.UI.CodeEditors;
using MathNet.Numerics.Distributions;

namespace Computator.NET.Data
{
    internal class AutocompletionData
    {
        public static AutocompleteItem[] GetAutocompleteItemsForExpressions(bool removeAdvanced=false)
        {
            var items = GetFunctionsNamesWithDescription(typeof(ElementaryFunctions));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(StatisticsFunctions)));


            items.AddRange(GetFunctionsNamesWithDescription(typeof(Normal), false, true));


            items.AddRange(GetFunctionsNamesWithDescription(typeof(Bernoulli), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Beta), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Binomial), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Categorical), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Cauchy), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Chi), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(ChiSquared), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(ContinuousUniform), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(ConwayMaxwellPoisson), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Dirichlet), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(DiscreteUniform), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Erlang), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Exponential), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(FisherSnedecor), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Gamma), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Geometric), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Hypergeometric), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(InverseGamma), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(InverseWishart), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Laplace), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(LogNormal), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(MatrixNormal), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Multinomial), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(NegativeBinomial), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(NormalGamma), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Pareto), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Poisson), false, true));

            items.AddRange(GetFunctionsNamesWithDescription(typeof(Rayleigh), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Stable), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(StudentT), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Triangular), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Weibull), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Wishart), false, true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(Zipf), false, true));


            items.AddRange(GetFunctionsNamesWithDescription(typeof(SpecialFunctions)));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(MathematicalConstants), true));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(PhysicalConstants), true));


            items.RemoveAll(i => i.Text == "ToCode");

            if (removeAdvanced)
            {
                items.RemoveAll(i => i.ImageIndex == -1);
            }

            return items.ToArray();
        }

        public static AutocompleteItem[] GetAutocompleteItemsForScripting()
        {
            var items = GetAutocompleteItemsForExpressions().ToList();

            items.AddRange(GetFunctionsNamesWithDescription(typeof(MatrixFunctions)));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(MathematicalTransformations)));
            items.AddRange(GetFunctionsNamesWithDescription(typeof(ScriptingFunctions)));
            items.AddRange(TslCompiler.Keywords.Select(s => new AutocompleteItem(s)));

            items.Sort((i1, i2) => i1.Text.CompareTo(i2.Text));
            items.ForEach(i => i.IsScripting=true);
            return items.ToArray();
        }

        public static List<CompletionData> ConvertAutocompleteItemsToCompletionDatas(AutocompleteItem[] autocompleteItems)
        {
            return autocompleteItems.Select(autocompleteItem => autocompleteItem.ToCompletionData()).ToList();
        }

        //TODO: differ menutext from text by adding types of arguments and maybe type of return
        //make it cleaner, nicer, apply recactorings
        //do extensive testing

        private static List<AutocompleteItem> GetFunctionsNamesWithDescription(Type type, bool noMethod = false,
            bool fullName = false)
        {
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            var items = new List<AutocompleteItem>();
            
            if (!noMethod)
                foreach (var m in methods)
                {
                    var fullNameExtension = "";
                    if (fullName)
                        fullNameExtension = m.ReflectedType.Name + ".";
                    
                    AddSignatureWithType(fullNameExtension + m.Name, MakeAddition(m, false), MakeAddition(m, true), m.ReturnType.Name,items);
                    AddMetadata(m, type, items);
                }

            foreach (var p in properties)
            {
                AddSignatureWithType(p.Name,"","", p.PropertyType.Name, items);
                AddMetadata(p, type, items);
            }

            foreach (var f in fields)
            {
                var argsCount = f.ToString().Count(c => c == ',');
                if (argsCount > 0)
                {
                    AnalyzeDelegateFields(f, type, items);
                }
                else
                {
                    AddSignatureWithType(f.Name,"","",  f.FieldType.Name, items);
                    AddMetadata(f, type, items);
                }
            }

            foreach (var t in type.GetNestedTypes())
            {
                items.AddRange(GetFunctionsNamesWithDescription(t, noMethod, fullName));
            }

            items.RemoveAll(i => i.Text == "ToCode");
            return items;
        }



        private static string MakeAddition(MethodInfo m, bool withType)
        {
            var parameters = m.GetParameters();
            var addition = "(";

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i < parameters.Length - 1)
                    addition += (withType) ? TypeNameToAlias(parameters[i].ParameterType.Name) + " " + parameters[i].Name + "," : parameters[i].Name + ",";
                else
                    addition += (withType) ? TypeNameToAlias(parameters[i].ParameterType.Name) + " " + parameters[i].Name : parameters[i].Name;
            }
            addition += ")";


            return addition;
        }


        private static void AddMetadata(MemberInfo p, Type type, List<AutocompleteItem> items)
        {

            if ((p.GetCustomAttributes(typeof(NameAttribute), false)).Any())

                items.Last().ToolTipTitle =
                    ((NameAttribute)(p.GetCustomAttributes(typeof(NameAttribute), false)[0])).Name;

            if ((p.GetCustomAttributes(typeof(DescriptionAttribute), false)).Any())
                items.Last().ToolTipText =
                (((DescriptionAttribute)(p.GetCustomAttributes(typeof(DescriptionAttribute), false)[0])))
                    .Description;
            if (items.Count > 0)
            {
                if (p.GetCustomAttributes(typeof(CategoryAttribute), false).Any())
                    items.Last().functionInfo.Category =
                        (((CategoryAttribute)(p.GetCustomAttributes(typeof(CategoryAttribute), false)[0]))).Category ??
                        "";

                items.Last().functionInfo.Signature = items.Last().Text ?? "";
                items.Last().functionInfo.Title = items.Last().ToolTipTitle ?? "";
                items.Last().functionInfo.Description = items.Last().ToolTipText ?? "";
                items.Last().functionInfo.Type = type.Name;
            }
        }

        public static string TypeNameToAlias(string typeName)
        {
            switch (typeName)
            {
                case "Double":
                    return "real";

                case "Int32":
                case "Int64":
                case "Int16":
                    return "integer";
                case "Uint32":
                case "Uint16":
                case "Uint64":
                    return "natural";

                case "Matrix":
                case "matrix":
                case "DenseMatrix":
                case "SparseMatrix":
                    return "Matrix";

                case "Void":
                case "String":
                case "Complex":
                    return typeName.ToLower();


                default:
                    return typeName;
            }
        }

        private static void AddSignatureWithType(string sigName, string typeName,
            List<AutocompleteItem> items)
        {
            AddSignatureWithType(sigName, sigName, typeName, items);
        }



        private static void AddSignatureWithType(string name, string addition, string additionWithType, string typeName,  List<AutocompleteItem> items)
        {
            var imageIndex = -1;
            switch (typeName)
            {
                case "Complex":
                    imageIndex = 1;
                    break;
                case "Double":
                case "T":
                    imageIndex = 0;
                    break;
                case "Int32":
                case "Int64":
                case "Int16":
                    imageIndex = 3;
                    break;
                case "Uint32":
                case "Uint16":
                case "Uint64":
                    imageIndex = 2;
                    break;
                case "Matrix":
                case "matrix":
                case "DenseMatrix":
                case "SparseMatrix":
                    imageIndex = 5;
                    break;
            }

            items.Add(new AutocompleteItem(name,addition,additionWithType,TypeNameToAlias(typeName),imageIndex));
        }


        private static void AddSignatureWithType(string sigName, string menuName, string typeName, List<AutocompleteItem> items)
        {
            switch (typeName)
            {
                case "Complex":
                    items.Add(new AutocompleteItem(sigName, 1, menuName));
                    break;
                case "Double":
                case "T":
                    items.Add(new AutocompleteItem(sigName, 0, menuName));
                    break;
                case "Int32":
                case "Int64":
                case "Int16":
                    items.Add(new AutocompleteItem(sigName, 3, menuName));
                    break;
                case "Uint32":
                case "Uint16":
                case "Uint64":
                    items.Add(new AutocompleteItem(sigName, 2, menuName));
                    break;
                case "Matrix":
                case "matrix":
                case "DenseMatrix":
                case "SparseMatrix":
                    items.Add(new AutocompleteItem(sigName, 5, menuName));
                    break;

                default:
                    items.Add(new AutocompleteItem(sigName, -1, menuName));
                    break;
            }
        }

        private static void AnalyzeDelegateFields(FieldInfo f, Type type, List<AutocompleteItem> items)
        {
            var addition = "(";

            //((GenericType)f.FieldType).GenericTypeArguments
            var argumentsTypes = (f.FieldType).GetGenericArguments();

            var method = f.FieldType.GetMethod("Invoke");

            var secondTime = false;
            for (var i = 0; i < argumentsTypes.Length - 1; i++)
            {
                if ((argumentsTypes[i].Name == "Double") && i < argumentsTypes.Length - 2)
                {
                    if (!addition.Contains("ν"))
                        addition += "ν,";
                    else if (!addition.Contains("μ"))
                        addition += "μ,";
                    else
                        addition += "α,";
                }


                if (((argumentsTypes[i].Name == "Int32") || (argumentsTypes[i].Name == "Uint32") ||
                     (argumentsTypes[i].Name == "Int64") || (argumentsTypes[i].Name == "Uint64")))
                {
                    if (!addition.Contains("n"))
                        addition += "n,";
                    else if (!addition.Contains("m"))
                        addition += "m,";
                    else
                        addition += "k,";
                }

                if ((argumentsTypes[i].Name == "Double" || argumentsTypes[i].Name == "T") &&
                    i == argumentsTypes.Length - 2)
                    addition += "x";

                if ((argumentsTypes[i].Name == "Complex") && i == argumentsTypes.Length - 2)
                    addition += "z";

                if (argumentsTypes[i].Name == "T")
                    secondTime = true;
            }
            addition += ")";


            if (method != null)
            {
                AddSignatureWithType(f.Name + addition, method.ReturnType.Name, items);
            }
            else
            {
                AddSignatureWithType(f.Name, f.FieldType.Name, items);
            }

            AddMetadata(f, type, items);

            if (secondTime)
            {
                AddSignatureWithType(f.Name + addition.Replace("x)", "z)"), "Complex", items);
                AddMetadata(f, type, items);
            }
        }




        /*
        public static string[] GetAutocompleteStrings()
        {
            var strings = GetFunctionsNames(typeof (ElementaryFunctions));
            strings.AddRange(GetFunctionsNames(typeof (SpecialFunctions)));
            strings.AddRange(GetFunctionsNames2014(typeof (StatisticsFunctions)));
            strings.AddRange(GetFunctionsNames(typeof (MathematicalConstants)));
            strings.AddRange(GetFunctionsNames(typeof (PhysicalConstants)));
            return strings.ToArray();
        }

        private static List<string> GetFunctionsNames2014(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            var names = new List<string>();

            foreach (var m in methods)
            {
                var parameters = m.GetParameters();
                var addition = "(";
                var secondTime = false;

                for (var i = 0; i < parameters.Length; i++)
                {
                    if ((parameters[i].ParameterType.Name == "Double") && i < parameters.Length - 1)
                    {
                        if (!addition.Contains("ν"))
                            addition += "ν,";
                        else if (!addition.Contains("μ"))
                            addition += "μ,";
                        else
                            addition += "α,";
                    }


                    if (((parameters[i].ParameterType.Name == "Int32") || (parameters[i].ParameterType.Name == "Uint32") ||
                         (parameters[i].ParameterType.Name == "Int64") || (parameters[i].ParameterType.Name == "Uint64")))
                    {
                        if (!addition.Contains("n"))
                            addition += "n,";
                        else if (!addition.Contains("m"))
                            addition += "m,";
                        else
                            addition += "k,";
                    }

                    if ((parameters[i].ParameterType.Name == "Double" || parameters[i].ParameterType.Name == "T") &&
                        i == parameters.Length - 1)
                        addition += "x";

                    if ((parameters[i].ParameterType.Name == "Complex") && i == parameters.Length - 1)
                        addition += "z";

                    if (parameters[i].ParameterType.Name == "T")
                        secondTime = true;
                }
                if (addition.Last() == ',')
                    addition = addition.Substring(0, addition.Length - 3) + ")";
                else
                    addition += ")";
                names.Add(m.Name + addition);

                if (secondTime)
                    names.Add(m.Name + addition.Replace("x)", "z)"));
            }

            foreach (var p in properties)
                names.Add(p.Name);

            foreach (var f in fields)
            {
                var argsCount = f.ToString().Count(c => c == ',');
                if (argsCount > 0)
                {
                    var addition = "(";

                    //((GenericType)f.FieldType).GenericTypeArguments
                    var argumentsTypes = (f.FieldType).GetGenericArguments();
                    var secondTime = false;
                    for (var i = 0; i < argumentsTypes.Length - 1; i++)
                    {
                        if ((argumentsTypes[i].Name == "Double") && i < argumentsTypes.Length - 2)
                        {
                            if (!addition.Contains("ν"))
                                addition += "ν,";
                            else if (!addition.Contains("μ"))
                                addition += "μ,";
                            else
                                addition += "α,";
                        }


                        if (((argumentsTypes[i].Name == "Int32") || (argumentsTypes[i].Name == "Uint32") ||
                             (argumentsTypes[i].Name == "Int64") || (argumentsTypes[i].Name == "Uint64")))
                        {
                            if (!addition.Contains("n"))
                                addition += "n,";
                            else if (!addition.Contains("m"))
                                addition += "m,";
                            else
                                addition += "k,";
                        }

                        if ((argumentsTypes[i].Name == "Double" || argumentsTypes[i].Name == "T") &&
                            i == argumentsTypes.Length - 2)
                            addition += "x";

                        if ((argumentsTypes[i].Name == "Complex") && i == argumentsTypes.Length - 2)
                            addition += "z";

                        if (argumentsTypes[i].Name == "T")
                            secondTime = true;
                    }
                    addition += ")";
                    names.Add(f.Name + addition);
                    if (secondTime)
                        names.Add(f.Name + addition.Replace("x)", "z)"));
                }
                else
                    names.Add(f.Name);
            }

            names.Remove("getFunctionsNames()");
            names.Remove("ToString()");
            names.Remove("get_i()");
            names.Remove("cmplxToMeta()");
            names.Remove("cmplxFromMeta()");

            var addMethodsList = new List<string>();

            foreach (var t in type.GetNestedTypes())
            {
                addMethodsList.AddRange(GetFunctionsNames2014(t));
            }
            names.AddRange(addMethodsList);

            return names;
        }

        private static List<string> GetFunctionsNames(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            var names = new List<string>();

            foreach (var m in methods)
            {
                var parameters = m.GetParameters();
                var addition = "(";
                var secondTime = false;

                for (var i = 0; i < parameters.Length; i++)
                {
                    if ((parameters[i].ParameterType.Name == "Double") && i < parameters.Length - 1)
                    {
                        if (!addition.Contains("ν"))
                            addition += "ν,";
                        else if (!addition.Contains("μ"))
                            addition += "μ,";
                        else
                            addition += "α,";
                    }


                    if (((parameters[i].ParameterType.Name == "Int32") || (parameters[i].ParameterType.Name == "Uint32") ||
                         (parameters[i].ParameterType.Name == "Int64") || (parameters[i].ParameterType.Name == "Uint64")))
                    {
                        if (!addition.Contains("n"))
                            addition += "n,";
                        else if (!addition.Contains("m"))
                            addition += "m,";
                        else
                            addition += "k,";
                    }

                    if ((parameters[i].ParameterType.Name == "Double" || parameters[i].ParameterType.Name == "T") &&
                        i == parameters.Length - 1)
                        addition += "x";

                    if ((parameters[i].ParameterType.Name == "Complex") && i == parameters.Length - 1)
                        addition += "z";

                    if (parameters[i].ParameterType.Name == "T")
                        secondTime = true;
                }
                if (addition.Last() == ',')
                    addition = addition.Substring(0, addition.Length - 3) + ")";
                else
                    addition += ")";
                names.Add(m.Name + addition);

                if (secondTime)
                    names.Add(m.Name + addition.Replace("x)", "z)"));
            }

            foreach (var p in properties)
                names.Add(p.Name);

            foreach (var f in fields)
            {
                var argsCount = f.ToString().Count(c => c == ',');
                if (argsCount > 0)
                {
                    var addition = "(";

                    //((GenericType)f.FieldType).GenericTypeArguments
                    var argumentsTypes = (f.FieldType).GetGenericArguments();
                    var secondTime = false;
                    for (var i = 0; i < argumentsTypes.Length - 1; i++)
                    {
                        if ((argumentsTypes[i].Name == "Double") && i < argumentsTypes.Length - 2)
                        {
                            if (!addition.Contains("ν"))
                                addition += "ν,";
                            else if (!addition.Contains("μ"))
                                addition += "μ,";
                            else
                                addition += "α,";
                        }


                        if (((argumentsTypes[i].Name == "Int32") || (argumentsTypes[i].Name == "Uint32") ||
                             (argumentsTypes[i].Name == "Int64") || (argumentsTypes[i].Name == "Uint64")))
                        {
                            if (!addition.Contains("n"))
                                addition += "n,";
                            else if (!addition.Contains("m"))
                                addition += "m,";
                            else
                                addition += "k,";
                        }

                        if ((argumentsTypes[i].Name == "Double" || argumentsTypes[i].Name == "T") &&
                            i == argumentsTypes.Length - 2)
                            addition += "x";

                        if ((argumentsTypes[i].Name == "Complex") && i == argumentsTypes.Length - 2)
                            addition += "z";

                        if (argumentsTypes[i].Name == "T")
                            secondTime = true;
                    }
                    addition += ")";
                    names.Add(f.Name + addition);
                    if (secondTime)
                        names.Add(f.Name + addition.Replace("x)", "z)"));
                }
                else
                    names.Add(f.Name);
            }

            names.Remove("getFunctionsNames()");
            names.Remove("ToString()");
            names.Remove("get_i()");
            names.Remove("cmplxToMeta()");
            names.Remove("cmplxFromMeta()");
            return names;
        }

        private static List<AutocompleteItem> GetFunctionsNamesEasy(Type type)
        {
            var list = new List<AutocompleteItem>();
            var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var prop in properties)
                list.Add(new AutocompleteItem(prop.Name));

            foreach (var f in fields)
                if (f.Name != "ToCode")
                    list.Add(new AutocompleteItem(f.Name));

            foreach (var m in methods)
            {
                var methodSig = new StringBuilder(m.Name + "(");

                var parameters = m.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    methodSig.Append(parameters[i].Name + ((i == parameters.Length - 1) ? "" : ","));
                }
                methodSig.Append(")");

                list.Add(new AutocompleteItem(methodSig.ToString()));
            }
            return list;
        }*/

    }
}