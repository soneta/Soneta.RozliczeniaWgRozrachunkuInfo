using Soneta.Business;
using Soneta.Kasa;
using Soneta.Ksiega;

namespace Soneta.RozliczeniaWgRozrachunkuInfo.Extender
{
    public partial class OpisAnalityczny2ViewInfo : ViewInfo
    {
        private OpisAnalityczny2.Params _pars;

        public OpisAnalityczny2ViewInfo(System.ComponentModel.IContainer container)
        {
            container.Add(this);
            InitContext += new ContextEventHandler(OpisAnalitycznyViewInfo_InitContext);
            CreateView += new CreateViewEventHandler(OpisAnalitycznyViewInfo_CreateView);
        }

        public OpisAnalityczny2ViewInfo()
        {
            InitContext += new ContextEventHandler(OpisAnalitycznyViewInfo_InitContext);
            CreateView += new CreateViewEventHandler(OpisAnalitycznyViewInfo_CreateView);
        }

        private void OpisAnalitycznyViewInfo_CreateView(object sender, CreateViewEventArgs args)
        {
            _pars = (OpisAnalityczny2.Params)args.Context[typeof(OpisAnalityczny2.Params)];
            args.View = _pars.CreateView();
            args.View.Condition &= _pars.CreateCondition();
            args.View.Condition &= _pars.CreateNewCondition();

            var oddziałParams = (OddzialParams)args.Context[typeof(OddzialParams)];
            args.View.Condition &= oddziałParams.GetCondition("Ewidencja.Oddzial");
        }

        private void OpisAnalitycznyViewInfo_InitContext(object sender, ContextEventArgs args)
        {
            ResourceName = "OpisAnalityczny2";
            args.Context.Set(new OpisAnalityczny2.Params(args.Context));
            args.Context.Set(new OddzialParams(args.Context));
            args.Context.Set(OpisAnalitycznyUID.ViewInfoZestawienieDokumentow);
        }

    }

}
