using SourceGenerator.Consoles.Builders.BizEnumExtendBuilder;
using System;

namespace SourceGenerator.Consoles;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        //var metaData = MetaDataHelper.LoadJson(
        //    @"F:\Code\git\SourceGenerator.Template\src\SourceGenerator.Console\obj\Generated\SourceGenerator.Analyzers\SourceGenerator.Analyzers.IncrementalGenerator\MetaJson.cs");

        //if (metaData != null)
        //{
        //    Console.WriteLine(metaData.AssemblyName);

        //    foreach (var classMetaData in metaData.ClassMetaDataList)
        //    {
        //        Console.WriteLine(classMetaData.Key);
        //    }
        //}

        new BizDictionaryClass().BindBizDepartment();

        Console.ReadKey();
    }
}