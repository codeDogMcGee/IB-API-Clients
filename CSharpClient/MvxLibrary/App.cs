using MvxLibrary.ViewModels;
using MvvmCross.ViewModels;

namespace MvxLibrary
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<StockTraderViewModel>();
        }
    }
}
