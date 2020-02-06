using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace OrganizadorChurrasco
{
    public partial class Form1 : Form
    {
        //String de conexão com o banco de dados.
        SqlConnection conexao = new SqlConnection(@"Data Source=.\SQLEXPRESS; Initial Catalog=BDCHURRASCO; User ID=sa; Password=sa");

        public Form1()
        {
            InitializeComponent();
            Carregar();
        }

        void Carregar()
        {
            try
            {
                conexao.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM TBITENS ORDER BY descricao", conexao);
                SqlDataAdapter da = new SqlDataAdapter(command);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvItens.DataSource = dt;

                if (dgvItens.SelectedRows.Count > 0)
                    dgvItens.CurrentRow.Selected = false;

                if (dt.Rows.Count > 0)
                {
                    // Faz a soma do total.
                    SqlCommand commandTotal = new SqlCommand("SELECT SUM(total) FROM TBITENS", conexao);
                    double retorno = Convert.ToDouble(commandTotal.ExecuteScalar());
                    txtTotal.Text = retorno.ToString("C");

                    //Contar os itens pendentes
                    SqlCommand commandPendentes = new SqlCommand("SELECT COUNT(codigo) FROM TBITENS WHERE status = 'Pendente'", conexao);
                    lblItensPendentes.Text = commandPendentes.ExecuteScalar().ToString();
                }


                SqlCommand infocommand = new SqlCommand("SELECT TOP 1 * FROM TBINFO", conexao);
                SqlDataAdapter dainfo = new SqlDataAdapter(infocommand);
                DataTable dtinfo = new DataTable();
                dainfo.Fill(dtinfo);

                foreach (DataRow item in dtinfo.Rows)
                {
                    mtcData.SelectionStart = Convert.ToDateTime(item["data"]);
                    dtpHora.Value = Convert.ToDateTime(item["hora"]);
                }
            }
            catch (Exception erro)
            {
                MessageBox.Show("Não foi possível selecionar os dados. Detalhes: " + erro.Message);
            }
            finally
            {
                conexao.Close();
            }
        }

        void LimparCampos()
        {
            txtItem.Clear();
            cbxUnidadeMedida.SelectedIndex = -1;
            nudQuantidade.Value = 1;
            txtPreco.Clear();
            txtItem.Focus();

            //Remove a seleção do datagrid para forçar o usuário a escolher um na hora remover.
            if (dgvItens.SelectedRows.Count > 0)
                dgvItens.CurrentRow.Selected = false;
        }


        private void btnAdicionar_Click(object sender, EventArgs e)
        {
            Adicionar();
        }

        void Adicionar()
        {
            if (string.IsNullOrWhiteSpace(txtItem.Text))
            {
                MessageBox.Show("Você precisa informar o NOME do Item!", "Op's!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtItem.Focus();
            }
            else if (cbxUnidadeMedida.SelectedIndex < 0)
            {
                MessageBox.Show("Você precisa selecionar uma UNIDADE DE MEDIDA!", "Op's!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cbxUnidadeMedida.Focus();
            }
            else if (!double.TryParse(txtPreco.Text, out double preco))
            {
                MessageBox.Show("Você precisa informar um PREÇO VÁLIDO!", "Op's!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPreco.Clear();
                txtPreco.Focus();
            }
            else
            {
                Produto produto = new Produto();
                produto.Descricao = txtItem.Text.Trim();
                produto.UnidadeMedida = cbxUnidadeMedida.SelectedItem.ToString();
                produto.Quantidade = (int)nudQuantidade.Value;
                produto.Preco = Convert.ToDouble(txtPreco.Text);
                produto.Status = "Pendente";



                //Atualiza o valor total.                
                txtTotal.Text = (Convert.ToDouble(txtTotal.Text.Substring(3)) + (produto.Quantidade * produto.Preco)).ToString("C");

                //Atualiza a quantidade de itens pendentes.                             
                lblItensPendentes.Text = (Convert.ToInt32(lblItensPendentes.Text) + 1).ToString();

                dgvItens.CurrentRow.Cells["status"].Style.ForeColor = Color.Red;
                LimparCampos();

                //Enviar para o banco de dados o item cadastrado.
                string query = "INSERT INTO TBITENS (descricao, unidademedida, quantidade, preco, total, status) VALUES (@descricao, @unidademedida, @quantidade, @preco, @total, @status); SELECT @@IDENTITY";

                SqlCommand command = new SqlCommand(query, conexao);
                command.Parameters.AddWithValue("@descricao", produto.Descricao);
                command.Parameters.AddWithValue("@unidademedida", produto.UnidadeMedida);
                command.Parameters.AddWithValue("@quantidade", produto.Quantidade);
                command.Parameters.AddWithValue("@preco", produto.Preco);
                command.Parameters.AddWithValue("@total", produto.Preco * produto.Quantidade);
                command.Parameters.AddWithValue("@status", produto.Status);
                command.CommandType = CommandType.Text;

                try
                {
                    conexao.Open();
                    int codigo = Convert.ToInt32(command.ExecuteScalar());

                    if (codigo > 0)
                    {
                        conexao.Close();
                        Carregar();
                        MessageBox.Show("Item cadastrado!");
                    }
                }
                catch (Exception erro)
                {
                    MessageBox.Show("Não foi possível cadastrar o item. Detalhes: " + erro.Message);
                }
                finally
                {
                    conexao.Close();
                }
            }
        }

        private void btnRemover_Click(object sender, EventArgs e)
        {
            Remover();
        }

        void Remover()
        {
            if (dgvItens.SelectedRows.Count == 0)
            {
                MessageBox.Show("Você precisa selecionar um item para remover", "Op's!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (MessageBox.Show("Deseja realmente remover este item da lista?", "Remover", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //Atualiza o valor total descontando o valor total do produto que será removido.
                    txtTotal.Text = (Convert.ToDouble(txtTotal.Text.Substring(3)) - Convert.ToDouble(dgvItens.CurrentRow.Cells["total"].Value.ToString().Substring(3))).ToString("C");

                    //Atualiza a quantidade de itens pendentes.
                    lblItensPendentes.Text = (Convert.ToInt32(lblItensPendentes.Text) - 1).ToString();

                    //Remove o item da lista.
                    dgvItens.Rows.Remove(dgvItens.CurrentRow);

                    LimparCampos();

                    // Remover no banco de dados.
                    try
                    {
                        conexao.Open();
                        string query = "DELETE FROM TBITENS WHERE codigo = " + dgvItens.CurrentRow.Cells["codigo"].Value.ToString();
                        SqlCommand command = new SqlCommand(query, conexao);

                        if (command.ExecuteNonQuery() > 0)
                        {
                            MessageBox.Show("Item Removido!");
                        }
                    }
                    catch (Exception erro)
                    {
                        MessageBox.Show(erro.Message);
                    }
                    finally
                    {
                        conexao.Close();
                    }
                }
            }
        }

        private void mtcData_DateChanged(object sender, DateRangeEventArgs e)
        {

        }

        private void dgvItens_DoubleClick(object sender, EventArgs e)
        {
            MudarStatus();
        }

        void MudarStatus()
        {
            string status;

            if (dgvItens.CurrentRow.Cells["status"].Value.ToString() == "Pendente")
                status = "Comprado";
            else
                status = "Pendente";

            string mensagem = "Mudar o status do produto " + dgvItens.CurrentRow.Cells["item"].Value.ToString().ToUpper() + " para " + status.ToUpper() + "?";

            DialogResult escolha = MessageBox.Show(mensagem, "STATUS DO ITEM", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (escolha == DialogResult.Yes)
            {
                dgvItens.CurrentRow.Cells["status"].Value = status;
                if (status == "Comprado")
                {
                    lblItensPendentes.Text = (Convert.ToInt32(lblItensPendentes.Text) - 1).ToString();
                    dgvItens.CurrentRow.Cells["status"].Style.ForeColor = Color.Green;
                }
                else
                {
                    lblItensPendentes.Text = (Convert.ToInt32(lblItensPendentes.Text) + 1).ToString();
                    dgvItens.CurrentRow.Cells["status"].Style.ForeColor = Color.Red;
                }

                LimparCampos();
                SqlCommand command = new SqlCommand("UPDATE TBITENS SET status = '" + status + "' WHERE codigo = " + dgvItens.CurrentRow.Cells["codigo"].Value.ToString(), conexao);
                conexao.Open();
                command.ExecuteNonQuery();
                conexao.Close();

            }
        }



        private void dgvItens_DoubleClick_1(object sender, EventArgs e)
        {
            MudarStatus();
        }

        private void mtcData_DateChanged_1(object sender, DateRangeEventArgs e)
        {
            if (mtcData.SelectionStart < DateTime.Today)
            {
                MessageBox.Show("Você não pode selecionar uma data anterior a data de hoje", "Op'!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                mtcData.SelectionStart = DateTime.Today;
            }
            else
            {
                lblDiasFaltam.Text = "faltam " + Convert.ToInt32((mtcData.SelectionStart - DateTime.Today).TotalDays).ToString() + " dias";

                try
                {
                    if (conexao.State == ConnectionState.Closed)
                        conexao.Open();

                    string query = "IF(NOT EXISTS(SELECT * FROM TBINFO WHERE data = '" + mtcData.SelectionStart + "')) BEGIN DELETE FROM TBINFO WHERE data <> '" + mtcData.SelectionStart + "'; INSERT INTO TBINFO (data, hora) VALUES (@data, @hora) END";

                    SqlCommand command = new SqlCommand(query, conexao);
                    command.Parameters.AddWithValue("@data", mtcData.SelectionStart);
                    command.Parameters.AddWithValue("@hora", dtpHora.Value.ToShortTimeString());
                    command.CommandType = CommandType.Text;

                    command.ExecuteNonQuery();
                }
                catch (Exception erro)
                {
                    MessageBox.Show("Algo deu errado. Detalhes: " + erro.Message);
                }
                finally
                {
                    conexao.Close();
                }
            }
        }
    }
}
