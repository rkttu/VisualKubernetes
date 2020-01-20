using System.Drawing;
using System.Windows.Forms;

namespace VisualKubernetes
{
    partial class MainForm
    {
        private ToolStripPanel bottomToolStrip;
        private StatusStrip statusStrip;

        private ToolStripStatusLabel statusLabel;

        private void InitializeBottomToolStripPanel()
        {
            bottomToolStrip = new ToolStripPanel()
            {
                Parent = this,
                Dock = DockStyle.Bottom,
            };

            statusStrip = new StatusStrip()
            {
                Parent = bottomToolStrip,
                Dock = DockStyle.Bottom,
                SizingGrip = true,
                GripStyle = ToolStripGripStyle.Visible,
            };
            bottomToolStrip.Join(statusStrip);

            statusLabel = new ToolStripStatusLabel()
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            statusStrip.Items.Add(statusLabel);
        }
    }
}
