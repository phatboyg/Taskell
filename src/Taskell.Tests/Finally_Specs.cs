// Copyright 2012-2013 Chris Patterson
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for
// the specific language governing permissions and limitations under the License.
namespace Taskell.Tests
{
    using System;
    using NUnit.Framework;


    [TestFixture]
    public class Using_finally
    {
        [Test]
        public void Should_always_be_called()
        {
            bool called = false;

            var composer = new TaskComposer<int>();

            composer.Execute(() => { throw new InvalidOperationException("This is expected"); });

            composer.Finally(status => { called = true; });

            Assert.Throws<AggregateException>(() => composer.Finish().Wait());

            Assert.IsTrue(called);
        }
    }
}