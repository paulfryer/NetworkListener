using Nancy;

namespace NetworkListener.Modules
{
    public abstract class CustomRequestVerbsModule : NancyModule
    {
        protected CustomRequestVerbsModule()
        {
        }

        protected CustomRequestVerbsModule(string modulePath) : base(modulePath)
        {
        }

        public RouteBuilder Notify
        {
            get { return new RouteBuilder("NOTIFY", this); }
        }
    }
}