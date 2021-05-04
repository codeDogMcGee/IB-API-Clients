using CSharpClient.MvxLibrary.ViewModels;
using MvvmCross.ViewModels;

namespace CSharpClient.MvxLibrary
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<StockTraderViewModel>();
        }
    }
}
