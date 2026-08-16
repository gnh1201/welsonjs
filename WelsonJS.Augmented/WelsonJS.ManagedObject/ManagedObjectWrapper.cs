// ManagedObjectWrapper.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX - FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Reflection;

namespace WelsonJS.ManagedObject
{
    public class ManagedObjectWrapper
    {
        private readonly object _target;

        public ManagedObjectWrapper(object target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            _target = target;
        }

        public object Invoke(string name, params object[] arguments)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (arguments == null)
                arguments = new object[0];

            object[] invokeArguments = new object[arguments.Length];
            ByRefArgument[] byRefArguments = new ByRefArgument[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                ByRefArgument byRef = arguments[i] as ByRefArgument;

                if (byRef != null)
                {
                    byRefArguments[i] = byRef;
                    invokeArguments[i] = byRef.Value;
                }
                else
                {
                    invokeArguments[i] = arguments[i];
                }
            }

            object result = _target.GetType().InvokeMember(
                name,
                BindingFlags.InvokeMethod |
                BindingFlags.Public |
                BindingFlags.Instance,
                null,
                _target,
                invokeArguments
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
            return _target.GetType().InvokeMember(
                name,
                BindingFlags.GetProperty,
                null,
                _target,
                null
            );
        }

        public void Set(string name, object value)
        {
            _target.GetType().InvokeMember(
                name,
                BindingFlags.SetProperty,
                null,
                _target,
                new object[] { value }
            );
        }
    }
}
