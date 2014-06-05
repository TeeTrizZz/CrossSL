﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CrossSL
{
    // ReSharper disable once InconsistentNaming
    internal static class xSLHelper
    {
        /// <summary>
        ///     Indicates if verbose mode is activ (i.e. if a .pdb file was found).
        /// </summary>
        internal static bool Verbose;

        /// <summary>
        ///     Resolves the type of a given TypeReference via Mono.Cecil and System.Reflection.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <returns>The resolved type.</returns>
        internal static Type ResolveRef(TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();
            var typeName = Assembly.CreateQualifiedName(typeDef.Module.Assembly.FullName, typeDef.FullName);
            return Type.GetType(typeName.Replace('/', '+'));
        }

        /// <summary>
        ///     Writes a message and  to console.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="findSeq">The instruction.</param>
        /// <remarks>
        ///     Some instructions have a SequencePoint which points to the specific line in
        ///     the source file. If the given line has no SequencePoint, this method will step
        ///     backwards until a SequencePoint is found (only if verbose mode is active).
        /// </remarks>
        internal static void WriteToConsole(string msg, Instruction findSeq)
        {
            Console.Write(msg);

            if (findSeq != null && Verbose)
            {
                while (findSeq.SequencePoint == null && findSeq.Previous != null)
                    findSeq = findSeq.Previous;

                if (findSeq.SequencePoint != null)
                {
                    var doc = findSeq.SequencePoint.Document.Url;
                    var line = findSeq.SequencePoint.StartLine;

                    Console.Write(" (" + Path.GetFileName(doc) + ":" + line + ")");
                }
            }

            Console.WriteLine(".");
        }
    }
}