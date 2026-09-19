using System.Windows.Forms;
using ICSharpCode.SharpDevelop.Gui;

namespace SelectiveBuild
{
    public class SelectiveBuildPad : AbstractPadContent
    {
        private SelectiveBuildControl _control;

        public override Control Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new SelectiveBuildControl();
                }
                return _control;
            }
        }

        public override void Dispose()
        {
            if (_control != null)
            {
                _control.Dispose();
                _control = null;
            }
            base.Dispose();
        }

        public override void RedrawContent()
        {
            if (_control != null) _control.Refresh();
        }
    }
}
