using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDE_UITest.UI
{
    public class ElementBase : AutomationElement
    {
        public ElementBase(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public T WaitForElement<T>(Func<T> getter, out bool isSuccess, int waitTimeout = 5)
        {
            var retry = Retry.WhileNull<T>(
                () => getter(),
                TimeSpan.FromSeconds(waitTimeout), interval: TimeSpan.FromSeconds(1));

            //Assert.True(retry.Success, $"Failed to get an element {getter} within a wait timeout");
            if (retry.Success)
            {
                isSuccess = true;
                return retry.Result;
            }
            else {
                isSuccess = false;
                return default(T);
            }
        }

        public T WaitForElement<T>(Func<T> getter, int waitTimeout = 5)
        {
            var retry = Retry.WhileNull<T>(
                () => getter(),
                TimeSpan.FromSeconds(waitTimeout), interval: TimeSpan.FromSeconds(1));

            //Assert.True(retry.Success, $"Failed to get an element {getter} within a wait timeout");
            
            return retry.Result;
            
        }
    }
}
