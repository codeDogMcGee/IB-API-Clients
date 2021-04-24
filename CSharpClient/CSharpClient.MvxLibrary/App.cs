using CSharpClient.MvxLibrary.ViewModels;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpClient.MvxLibrary
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<ContractPickerViewModel>();
        }
    }
}
