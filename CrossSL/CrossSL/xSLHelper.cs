using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossSL
{
    // ReSharper disable once InconsistentNaming
    internal static class xSLHelper
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
        ///     Extension for TypeReference:
        ///         Resolves the type of a given TypeReference.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <returns>The resolved type or <see cref="System.Object" /> if type is unknown.</returns>
        internal static Type ToType(this TypeReference typeRef)
        {
            return typeRef.Resolve().ToType();
        }

        /// <summary>
        ///     Extension for TypeDefinion:
        ///         Resolves the type of a given TypeDefinition.
        /// </summary>
        /// <param name="typeDef">The type definition to resolve.</param>
        /// <returns>The resolved type or <see cref="System.Object" /> if type is unknown.</returns>
        internal static Type ToType(this TypeDefinition typeDef)
        {
            var fullName = typeDef.Module.Assembly.FullName;
            var typeName = Assembly.CreateQualifiedName(fullName, typeDef.FullName);
            return Type.GetType(typeName.Replace('/', '+')) ?? typeof (Object);
        }

        /// <summary>
        ///     Extension for TypeReference:
        ///         Determines whether the given TypeReference is of a specific type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="typeRef">The TypeReference to compare.</param>
        /// <returns></returns>
        internal static bool IsType<T>(this TypeReference typeRef)
        {
            return (typeRef.ToType() == typeof (T));
        }

        /// <summary>
        ///     Extension for TypeDefinion:
        ///         Determines whether the given TypeDefinition is of a specific type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="typeDef">The TypeDefinition to compare.</param>
        /// <returns></returns>
        internal static bool IsType<T>(this TypeDefinition typeDef)
        {
            return (typeDef.ToType() == typeof(T));
        }

        /// <summary>
        ///     Prints a message to the console.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="findSeq">The corresponding instruction.</param>
        /// <remarks>
        ///     Some instructions have a SequencePoint which points to the specific line in
        ///     the source file. If the given line has no SequencePoint, this method will step
        ///     backwards until a SequencePoint is found (only if verbose mode is active).
        /// </remarks>
        private static void WriteToConsole(string msg, Instruction findSeq)
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
            WriteToConsole(msg, findSeq);

            Abort = true;
        }
    }
}