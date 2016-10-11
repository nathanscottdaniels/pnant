// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
//
// Clayton Harbour (claytonharbour@sporadicism.com)

using NUnit.Framework;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Test that cpp projects are built successfully.
    /// </summary>
    [TestFixture]
    public class CppSolutionTests : SolutionTestBase {
        // add fields here
        /// <summary>
        /// LanguageType that is being tested.
        /// </summary>
        protected override LanguageType CurrentLanguage {
            get {return LanguageType.cpp;}
        }
        /// <summary>
        /// Initialize example directory.
        /// </summary>
        public CppSolutionTests () {
        }

        /// <summary>
        /// Run the checkout command so we have something to update.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown();
        }
        /// <summary>
        /// Tests that the winforms solution builds using the nant solution task.  Ensures that
        /// the outputs are generated correctly.
        /// </summary>
        [Test]
        [Ignore("Does not work if path not setup correctly.")]
        public void TestWinForm () {
            this.RunTestPlain();
        }
    }
}
