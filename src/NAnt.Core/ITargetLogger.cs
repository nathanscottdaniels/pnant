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
namespace NAnt.Core
{
    /// <summary>
    /// Defines an interface that Targets will use in order to log messagaes to the console/file/whatever
    /// </summary>
    public interface ITargetLogger
    {
        /// <summary>
        /// Writes a <see cref="Project" /> level message to the build log with
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Level messageLevel, string message);

        /// <summary>
        /// Writes a <see cref="Project" /> level formatted message to the build 
        /// log with the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        void Log(Level messageLevel, string message, params object[] args);

        /// <summary>
        /// Writes a <see cref="Task" /> task level message to the build log 
        /// with the given <see cref="Level" />.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> from which the message originated.</param>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Task task, Level messageLevel, string message);

        /// <summary>
        /// Writes a <see cref="Target" /> level message to the build log with 
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> from which the message orignated.</param>
        /// <param name="messageLevel">The level to log at.</param>
        /// <param name="message">The message to log.</param>
        void Log(Target target, Level messageLevel, string message);

        /// <summary>
        /// Increases the indentation level of the log
        /// </summary>
        void Indent();

        /// <summary>
        /// Decreases 
        /// </summary>
        void Unindent();
    }
}
