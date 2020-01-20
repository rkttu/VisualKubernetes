using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace VisualKubernetes
{
    [DesignerCategory("")]
    internal sealed partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponents();
            InitializeFormDesign();
        }

        private IContainer container;

        private void InitializeComponents()
        {
            container = new Container();
        }

        private void InitializeFormDesign()
        {
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Size = new Size(640, 480);
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            IsMdiContainer = true;
            DoubleBuffered = true;
            Text = "Visual Kubernetes";

            InitializeTopToolStripPanel();
            InitializeBottomToolStripPanel();

            ResumeLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (container != null)
                {
                    container.Dispose();
                    container = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
