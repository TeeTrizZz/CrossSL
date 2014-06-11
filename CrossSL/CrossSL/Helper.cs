﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossSL
{
    // ReSharper disable once InconsistentNaming
    internal static class Helper
    {
        /// <summary>
        ///     Indicates if verbose mode is activ (i.e. if a .pdb file was found).
        /// </summary>
        internal static bool Verbose { get; set; }

        /// <summary>
        ///     Indicates if an error was raised and the compiler has to abort.
        /// </summary>
        internal static bool Abort { get; set; }

        /// <summary>
        ///     Collects all error messages for further processing.
        /// </summary>
        internal static StringBuilder Errors { get; set; }

        /// <summary>
        ///     Extension for TypeReference:
        ///         Resolves the type of a given <see cref="TypeReference" />.
        /// </summary>
        /// <param name="typeRef">The <see cref="TypeReference" /> to resolve.</param>
        /// <returns>The resolved type or <see cref="System.Object" /> if type is unknown.</returns>
        internal static Type ToType(this TypeReference typeRef)
        {
            return typeRef.Resolve().ToType();
        }

        /// <summary>
        ///     Extension for TypeDefinion:
        ///         Resolves the type of a given <see cref="TypeDefinition" />.
        /// </summary>
        /// <param name="typeDef">The <see cref="TypeDefinition" /> to resolve.</param>
        /// <returns>The resolved type or <see cref="System.Object" /> if type is unknown.</returns>
        internal static Type ToType(this TypeDefinition typeDef)
        {
            var fullName = typeDef.Module.Assembly.FullName;
            var typeName = Assembly.CreateQualifiedName(fullName, typeDef.FullName);
            return Type.GetType(typeName.Replace('/', '+')) ?? typeof (Object);
        }

        /// <summary>
        ///     Extension for TypeReference:
        ///         Determines whether the given <see cref="TypeReference" /> is of a specific type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="typeRef">The <see cref="TypeReference" /> to compare.</param>
        /// <returns></returns>
        internal static bool IsType<T>(this TypeReference typeRef)
        {
            return (typeRef.ToType() == typeof (T));
        }

        /// <summary>
        ///     Extension for TypeDefinion:
        ///         Determines whether the given <see cref="TypeDefinition" /> is of a specific type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="typeDef">The <see cref="TypeDefinition" /> to compare.</param>
        /// <returns></returns>
        internal static bool IsType<T>(this TypeDefinition typeDef)
        {
            return (typeDef.ToType() == typeof(T));
        }

        /// <summary>
        ///     Extension for Expression:
        ///         Determines whether the given <see cref="Expression" /> is of a specific type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="expr">The <see cref="Expression" /> to compare.</param>
        /// <returns></returns>
        internal static bool IsType<T>(this Expression expr)
        {
            return (expr.GetType() == typeof(T));
        }

        /// <summary>
        /// Prints a message to the console.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="findSeq">The corresponding instruction.</param>
        /// <returns>The messages posted to the console.</returns>
        /// <remarks>
        /// Some instructions have a SequencePoint which points to the specific line in
        /// the source file. If the given line has no SequencePoint, this method will step
        /// backwards until a SequencePoint is found (only if verbose mode is active).
        /// </remarks>
        private static string WriteToConsole(string msg, Instruction findSeq)
        {
            var message = new StringBuilder(msg);

            if (findSeq != null && Verbose)
            {
                while (findSeq.SequencePoint == null && findSeq.Previous != null)
                    findSeq = findSeq.Previous;

                if (findSeq.SequencePoint != null)
                {
                    var doc = findSeq.SequencePoint.Document.Url;
                    var line = findSeq.SequencePoint.StartLine;
                    var colmn = findSeq.SequencePoint.StartColumn;

                    message.Append(" (" + Path.GetFileName(doc) + "(" + line + "," + colmn + "))");
                }
            }

            Console.WriteLine(message.Dot());
            return message.ToString();
        }

        /// <summary>
        ///     Prints a warning to the console.
        /// </summary>
        /// <param name="msg">The warning message.</param>
        /// <param name="findSeq">The corresponding instruction.</param>
        internal static void Warning(string msg, Instruction findSeq = null)
        {
            msg = "    => WARNING: " + msg;
            WriteToConsole(msg, findSeq);
        }

        /// <summary>
        ///     Prints an error to the console and sets <see cref="Abort" /> to [true].
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="findSeq">The corresponding instruction.</param>
        internal static void Error(string msg, Instruction findSeq = null)
        {
            msg = "    => ERROR:   " + msg; 
            Errors.Append(WriteToConsole(msg, findSeq)).NewLine();

            Abort = true;
        }

        /// <summary>
        /// Resets <see cref="Abort"/> and <see cref="Errors"/> fields of this class.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void Reset()
        {
            Abort = false;
            Errors = new StringBuilder();
        }
    }
}