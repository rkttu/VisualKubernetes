using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRoleBindingResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1RoleBindingList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker roleBindingsLoader;

        private void InitializeRoleBindingLoader()
        {
            roleBindingsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            roleBindingsLoader.DoWork += RoleBindingsLoader_DoWork;
            roleBindingsLoader.RunWorkerCompleted += RoleBindingsLoader_RunWorkerCompleted;
            container.Add(roleBindingsLoader);
        }

        private void RoleBindingsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandRoleBindingResponse(
                args.Item1,
                client.ListNamespacedRoleBinding(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void RoleBindingsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load role bindings due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load role bindings due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandRoleBindingResponse;
            var roleBindingList = response.Item2;
            if (roleBindingList == null)
            {
                MessageBox.Show(this,
                    "Cannot load role bindings due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var roleBindingsTreeNode = response.Item3;
            roleBindingsTreeNode.Nodes.Clear();
            var roleBindingNodes = new List<TreeNode>();
            for (int i = 0, max = roleBindingList.Items.Count; i < max; i++)
            {
                var eachRoleBindingTreeNode = new TreeNode(roleBindingList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, roleBindingList.Items[i]),
                    ImageKey = "rolebinding",
                    SelectedImageKey = "rolebinding",
                };
                roleBindingNodes.Add(eachRoleBindingTreeNode);
            }
            roleBindingsTreeNode.Nodes.AddRange(roleBindingNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1RoleBinding o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedRoleBindingAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Role Binding - {fetched.Metadata.Name}";
        }
    }
}
