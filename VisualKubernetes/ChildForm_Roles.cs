using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRoleResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1RoleList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker rolesLoader;

        private void InitializeRoleLoader()
        {
            rolesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            rolesLoader.DoWork += RolesLoader_DoWork;
            rolesLoader.RunWorkerCompleted += RolesLoader_RunWorkerCompleted;
            container.Add(rolesLoader);
        }

        private void RolesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandRoleResponse(
                args.Item1,
                client.ListNamespacedRole(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void RolesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load roles due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load roles due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandRoleResponse;
            var roleList = response.Item2;
            if (roleList == null)
            {
                MessageBox.Show(this,
                    "Cannot load roles due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var rolesTreeNode = response.Item3;
            rolesTreeNode.Nodes.Clear();
            var roleNodes = new List<TreeNode>();
            for (int i = 0, max = roleList.Items.Count; i < max; i++)
            {
                var eachRoleTreeNode = new TreeNode(roleList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, roleList.Items[i]),
                    ImageKey = "role",
                    SelectedImageKey = "role",
                };
                roleNodes.Add(eachRoleTreeNode);
            }
            rolesTreeNode.Nodes.AddRange(roleNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Role o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedRoleAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Role - {fetched.Metadata.Name}";
        }
    }
}
