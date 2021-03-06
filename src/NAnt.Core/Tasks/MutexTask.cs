﻿// pNAnt - A parallel .NET build tool
// Copyright (C) 2016 Nathan Daniels
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using NAnt.Core.Attributes;
using System;
using System.Threading;

namespace NAnt.Core.Tasks
{
    /// <summary>
    /// The mutex task will execute its contents once it obtains a handle on the system mutex specified
    /// by <see cref="Name">name</see>.  In order to prevent endless builds, the build will fail if the
    /// mutex is not obtained within <see cref="Timeout"/> seconds.
    /// </summary>
    [TaskName("mutex")]
    public class MutexTask : TaskContainer
    {
        /// <summary>
        /// The name of the mutext to wait for
        /// </summary>
        [TaskAttribute("name", ExpandProperties = true, Required = true)]
        [StringValidator(AllowEmpty = false)]
        public new String Name { get; set; }

        /// <summary>
        /// The amount of time to wait for the mutext to become available before failing the build
        /// </summary>
        [TaskAttribute("timeout", Required = true)]
        [Int32Validator(MinValue = 1)]
        public Int32 Timeout { get; set; } 

        /// <summary>
        /// The mutex to use
        /// </summary>
        private Mutex chosenMutex;

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            this.CheckForDeadlocks();

            if (!this.chosenMutex.WaitOne(new TimeSpan(0, 0, this.Timeout)))
            {
                throw new BuildException(String.Format("Timeout expired while waiting for mutext \"{0}\".  The timeout was {1} seconds.", this.Name, this.Timeout));
            }

            try
            {
                base.ExecuteTask();
            }
            finally
            {
                this.chosenMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Initializes this task
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.chosenMutex = new Mutex(false, this.Name);
        }

        /// <summary>
        /// Attempts to check for possible deadlocks by looking up the call chain to see if a parent is holding this mutex
        /// </summary>
        /// <exception cref="BuildException">If a deadlock is detected</exception>
        private void CheckForDeadlocks()
        {
            foreach (var ancestor in this.CallStack.GetEntireTaskAncestry())
            {
                var task = ancestor.Task as MutexTask;
                if (task != null && task != this)
                {
                    if (task.Name.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new BuildException(String.Format("Deadlock detected!  A target is currently waiting for mutex \"{0}\" which is held by a parent task.", this.Name));
                    }
                }
            }
        }
    }
}
