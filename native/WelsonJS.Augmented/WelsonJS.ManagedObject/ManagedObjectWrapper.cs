// ManagedObjectWrapper.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    // Returned to scripts by ManagedObject.Wrap(). It must be COM visible,
    // otherwise scripts cannot call Call/CallWithArray/Get/Set through IDispatch.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class ManagedObjectWrapper
    {
        private const BindingFlags MemberBindingFlags =
            BindingFlags.Public | BindingFlags.Instance;

        private readonly object _target;

        public ManagedObjectWrapper(object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            _target = target;
        }

        // The wrapped object, for calling members that need no ByRef handling.
        public object Target
        {
            get { return _target; }
        }

        // Calls a method. Arguments that are ByRefArgument objects are passed
        // by reference, and their Value is updated after the call.
        //
        // Named "Call" rather than "Invoke": a member named Invoke collides
        // with IDispatch::Invoke and cannot be reached by script clients.
        public object Call(string name, params object[] arguments)
        {
            return CallCore(name, arguments);
        }

        // Same as Call, but takes the arguments as one array, so script
        // clients can pass any number of arguments without spreading them.
        // Accepts a JScript array, a SAFEARRAY (e.g. VBArray or
        // Scripting.Dictionary.Items()), or null for no arguments.
        public object CallWithArray(string name, [Optional] object arguments)
        {
            object[] argumentArray = ToArgumentArray(arguments);

            // A JScript array arrives as a COM object (an RCW of its own).
            RcwCollector.Observe(arguments);

            return CallCore(name, argumentArray);
        }

        private object CallCore(string name, object[] arguments)
        {
            ValidateMemberName(name);

            if (arguments == null)
                arguments = new object[0];

            object[] invokeArguments = new object[arguments.Length];
            ByRefArgument[] byRefArguments = new ByRefArgument[arguments.Length];
            bool hasByRef = false;

            for (int i = 0; i < arguments.Length; i++)
            {
                ByRefArgument byRef = arguments[i] as ByRefArgument;

                if (byRef != null)
                {
                    byRefArguments[i] = byRef;
                    invokeArguments[i] = byRef.Value;
                    hasByRef = true;
                }
                else
                {
                    invokeArguments[i] = arguments[i];
                }
            }

            // COM objects are invoked through IDispatch, which only passes
            // arguments by reference (VT_BYREF) when a ParameterModifier marks
            // them. Without it, values modified by the callee are not copied
            // back. Managed targets copy ref/out values back without it.
            ParameterModifier[] modifiers = null;
            if (hasByRef && invokeArguments.Length > 0)
            {
                ParameterModifier modifier = new ParameterModifier(invokeArguments.Length);
                for (int i = 0; i < invokeArguments.Length; i++)
                    modifier[i] = byRefArguments[i] != null;

                modifiers = new[] { modifier };
            }

            // Like JScript and VBScript call syntax (DISPATCH_METHOD |
            // DISPATCH_PROPERTYGET), so parameterized and default properties
            // such as Dictionary.Item(key) can be called as well as methods.
            object result = InvokeMember(
                name,
                BindingFlags.InvokeMethod | BindingFlags.GetProperty,
                invokeArguments,
                modifiers,
                "call method"
            );

            // Copy values modified through ByRef parameters back
            // to their corresponding ByRefArgument objects.
            for (int i = 0; i < invokeArguments.Length; i++)
            {
                if (byRefArguments[i] != null)
                    byRefArguments[i].Value = invokeArguments[i];
            }

            return result;
        }

        public object Get(string name)
        {
            ValidateMemberName(name);

            return InvokeMember(
                name,
                BindingFlags.GetProperty,
                null,
                null,
                "get property"
            );
        }

        public void Set(string name, object value)
        {
            ValidateMemberName(name);

            InvokeMember(
                name,
                BindingFlags.SetProperty,
                new object[] { value },
                null,
                "set property"
            );
        }

        private object InvokeMember(
            string name,
            BindingFlags invokeFlags,
            object[] arguments,
            ParameterModifier[] modifiers,
            string operation)
        {
            try
            {
                object result = _target.GetType().InvokeMember(
                    name,
                    invokeFlags | MemberBindingFlags,
                    null,
                    _target,
                    arguments,
                    modifiers,
                    CultureInfo.InvariantCulture,
                    null
                );

                // COM objects passed in, returned, or written back through
                // ByRef arguments are held by RCWs; see RcwCollector.
                RcwCollector.Observe(arguments);
                RcwCollector.Observe(result);

                return result;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw CreateMemberException(name, operation, ex.InnerException);
            }
            catch (Exception ex) when (
                ex is MissingMemberException ||
                ex is AmbiguousMatchException ||
                ex is ArgumentException ||
                ex is COMException)
            {
                throw CreateMemberException(name, operation, ex);
            }
        }

        private COMException CreateMemberException(string name, string operation, Exception inner)
        {
            string message = "Failed to " + operation + " \"" + name + "\" on " +
                DescribeTarget() + ": " + inner.Message;

            COMException comException = inner as COMException;
            return comException != null
                ? new COMException(message, comException.ErrorCode)
                : new COMException(message, inner);
        }

        private string DescribeTarget()
        {
            return Marshal.IsComObject(_target)
                ? "COM object"
                : _target.GetType().FullName;
        }

        private static object[] ToArgumentArray(object arguments)
        {
            if (arguments == null || arguments is Missing || arguments is DBNull)
                return new object[0];

            object[] objectArray = arguments as object[];
            if (objectArray != null)
                return objectArray;

            Array array = arguments as Array;
            if (array != null)
            {
                if (array.Rank != 1)
                    throw new ArgumentException("The argument array must be one-dimensional.", nameof(arguments));

                object[] result = new object[array.Length];
                int lowerBound = array.GetLowerBound(0);
                for (int i = 0; i < result.Length; i++)
                    result[i] = array.GetValue(lowerBound + i);
                return result;
            }

            // A JScript array arrives as an IDispatch object; read its
            // "length" property and indexed elements through late binding.
            if (Marshal.IsComObject(arguments))
            {
                Type type = arguments.GetType();
                int length;
                try
                {
                    length = Convert.ToInt32(type.InvokeMember(
                        "length", BindingFlags.GetProperty, null, arguments, null,
                        CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        "The arguments must be an array (an object with a length property). " + ex.Message,
                        nameof(arguments));
                }

                if (length < 0)
                    throw new ArgumentException("The argument array has an invalid length.", nameof(arguments));

                object[] result = new object[length];
                for (int i = 0; i < length; i++)
                {
                    result[i] = type.InvokeMember(
                        i.ToString(CultureInfo.InvariantCulture), BindingFlags.GetProperty, null, arguments, null,
                        CultureInfo.InvariantCulture);
                }
                return result;
            }

            throw new ArgumentException(
                "The arguments must be an array. Received: " + arguments.GetType().FullName,
                nameof(arguments));
        }

        private static void ValidateMemberName(string name)
        {
            if (name == null || name.Trim().Length == 0)
                throw new ArgumentException("A member name is required.", nameof(name));
        }
    }
}
