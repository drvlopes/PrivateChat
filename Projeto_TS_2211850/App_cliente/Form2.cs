using EI.SI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace App_cliente
{
    public partial class Form2 : Form
    {
        NetworkStream networkStream;
        ProtocolSI protocolSI;
        TcpClient client;
        int numMensagem = 0; //Ao estar utilizar treeview necessito saber quantas mensagens foram apresentadas para incrementar a seguinte
        string username;
        AesCryptoServiceProvider aes;
        RSACryptoServiceProvider rsa;
        string publickey;
        Thread ctThread;

        public Form2(TcpClient client, string username, byte[] passAES)
        {
            InitializeComponent();
            this.client = client;
            this.username = username;
            networkStream = client.GetStream();
            protocolSI = new ProtocolSI();
            aes = new AesCryptoServiceProvider();
            rsa = new RSACryptoServiceProvider();

            publickey = rsa.ToXmlString(false); // Chave Pública
            string bothkeys = rsa.ToXmlString(true); // Chave Privada + Pública

            aes.Key = GerarChavePrivada(passAES);
            aes.IV = GerarIv(passAES);

            ctThread = new Thread(getMessage);
            ctThread.Start();
        }

        private void bt_enviar_Click(object sender, EventArgs e)
        {
            string mensagem = tb_mensagem.Text.Trim();
            if (mensagem == "")
                return;

            tb_mensagem.Clear();
            byte[] signature = signData(mensagem);

            JArray array = new JArray();
            array.Add(mensagem);
            array.Add(signature);

            byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, cifrarDados(array.ToString()));
            networkStream.Write(packet, 0, packet.Length);
            networkStream.Flush();
            AddMensagem(mensagem, true);
        }

        private void addNodes(string mensagem, int same)
        {
            if (mensagem.Length < 60)
            {
                tv_historico.Nodes[numMensagem - same].Nodes.Add(mensagem);
            }
            else
            {
                foreach (var item in StringWrap(mensagem))
                {
                    tv_historico.Nodes[numMensagem - same].Nodes.Add(item);
                }
            }
            tv_historico.TopNode = tv_historico.Nodes[tv_historico.Nodes.Count - 1].LastNode;
        }

        public void AddMensagem(string mensagem, bool myself)
        {
            if (this.InvokeRequired)//Permite o acesso à treeview de outra thread
                this.Invoke((MethodInvoker)delegate
                {
                    AddMensagem(mensagem, myself);
                });
            else
            {
                if (myself)//apresenta a mensagem enviada pelo proprio utilizador
                {
                    var teste = numMensagem >0 ? tv_historico.Nodes[numMensagem - 1]: null;
                    if (teste != null ? teste.Text == username.ToString(): false)
                    {
                        addNodes(mensagem, 1);
                        tv_historico.ExpandAll();
                        return;
                    }
                    else
                    {
                        tv_historico.Nodes.Add(username.ToString()).ForeColor = Color.Green;
                        addNodes(mensagem, 0);
                    }
                }
                else
                {
                    JArray utilizador = new JArray();
                    utilizador = JsonConvert.DeserializeObject<JArray>(mensagem);//coverte em array [0]=utilizador, [1]=mensagem, [2]=true-> informação do servidor
                    if(utilizador[2].ToObject<bool>() == true)
                    {
                        tv_historico.Nodes.Add(utilizador[1].ToString()).ForeColor = Color.Red;
                        tv_historico.TopNode = tv_historico.Nodes[tv_historico.Nodes.Count - 1].LastNode;
                    }
                    else
                    {
                        var teste = numMensagem > 0 ? tv_historico.Nodes[numMensagem - 1] : null;
                        if (teste != null ? teste.Text == utilizador[0].ToString() : false)
                        {
                            addNodes(utilizador[1].ToString(), 1);
                            tv_historico.ExpandAll();
                            return;
                        }
                        else
                        {
                            tv_historico.Nodes.Add(utilizador[0].ToString()).ForeColor = Color.Blue;
                            addNodes(utilizador[1].ToString(), 0);
                        }
                    }
                }
                tv_historico.ExpandAll();
                numMensagem++;
            }
        }

        private IEnumerable<string> StringWrap(string mensagem)//função para resolver limitação da treeview(ao passar o limite de caracteres da mesma ficava com scroll horizontal
        {
            var splits = mensagem.Trim().Split(' ');
            var number = splits.Count();
            if(number == 1)
            {
                string[] sub = new string[2];
                sub[0] = mensagem.Substring(0, (int)(mensagem.Length / 2));
                sub[1] = mensagem.Substring((int)(mensagem.Length / 2), (int)(mensagem.Length / 2));

                for (int j = 0; j < 2; j++)
                {
                    yield return sub[j];
                }
            }
            else
            {
                string msg;
                int i = 0;
                while (i < number)
                {
                    msg = "";
                    do
                    {
                        msg += splits[i];
                        msg += " ";
                        i++;
                    } while (msg.Length <= 60 && i < number);

                    yield return msg;//Permite devolver a mensagem em linhas separadas
                }
            }
        }

        private void getMessage()//função que é instaciada na thread para receber as mensagens enviadas
        {
            //enviar chave publica(rsa)
            byte[] dados = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, cifrarDados(publickey));
            networkStream.Write(dados, 0, dados.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }

            while (true)
            {
                try
                {
                    networkStream = null;
                    networkStream = client.GetStream();

                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                    if(protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
                    {

                        string mensagem = decifrar(protocolSI.GetData());

                        AddMensagem(mensagem, false);
                    }
                }
                catch (Exception ex)
                {
                    if(networkStream == null)
                    {
                        if(!(ex.Message == "Cannot access a disposed object.\r\nObject name: 'System.Net.Sockets.TcpClient'."))
                        MessageBox.Show("Falha na ligação com o servidor!");
                        Application.Exit();
                        ctThread.Abort();
                    }
                }
            }
        }

        private void tb_mensagem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                bt_enviar_Click(sender, e);
            }
        }

        private byte[] GerarChavePrivada(byte[] pass)
        {
            // O salt, explicado de seguida tem de ter no mínimo 8 bytes e não
            //é mais do que array be bytes. O array é caracterizado pelo []
            byte[] salt = new byte[] { 6, 5, 2, 2, 4, 9, 0, 5 };
            /* A Classe Rfc2898DeriveBytes é um método para criar uma chave e um vector de inicialização.
				Esta classe usa:
				pass = password usada para derivar a chave;
				salt = dados aleatório usado como entrada adicional. É usado para proteger password.
				1000 = número mínimo de iterações recomendadas
			*/
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(pass, salt, 1337);
            //GERAR KEY
            return pwdGen.GetBytes(16);
        }

        private byte[] GerarIv(byte[] pass)
        {
            byte[] salt = new byte[] { 7, 8, 7, 8, 2, 5, 9, 5 };
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(pass, salt, 1337);
            //GERAR UMA KEY
            return pwdGen.GetBytes(16);
        }

        private byte[] cifrarDados(string txt)
        {
            //VARIÁVEL PARA GUARDAR O TEXTO DECIFRADO EM BYTES
            byte[] txtDecifrado = Encoding.UTF8.GetBytes(txt);
            //VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
            byte[] txtCifrado;
            //RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
            MemoryStream ms = new MemoryStream();
            //INICIALIZAR O SISTEMA DE CIFRAGEM (WRITE)
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            //CRIFRAR OS DADOS
            cs.Write(txtDecifrado, 0, txtDecifrado.Length);
            cs.Close();
            //GUARDAR OS DADOS CRIFRADO QUE ESTÃO NA MEMÓRIA
            txtCifrado = ms.ToArray();

            return txtCifrado;
        }

        private string decifrar(byte[] txtCifrado)
        {
            //RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
            MemoryStream ms = new MemoryStream(txtCifrado);
            //INICIALIZAR O SISTEMA DE CIFRAGEM (READ)
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            //VARIÁVEL PARA GUARDO O TEXTO DECIFRADO
            byte[] txtDecifrado = new byte[ms.Length];
            //VARIÁVEL PARA TER O NÚMERO DE BYTES DECIFRADOS
            int bytesLidos = 0;
            //DECIFRAR OS DADOS
            bytesLidos = cs.Read(txtDecifrado, 0, txtDecifrado.Length);
            cs.Close();
            //CONVERTER PARA TEXTO
            string textoDecifrado = Encoding.UTF8.GetString(txtDecifrado, 0, bytesLidos);
            //DEVOLVER TEXTO DECRIFRADO
            return textoDecifrado;
        }

        private byte[] signData(string data)
        {
            byte[] dados = Encoding.UTF8.GetBytes(data);

            using (SHA1 sha1 = SHA1.Create())
            {
                return rsa.SignData(dados, sha1);
            }
        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            byte[] dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_9);
            networkStream.Write(dados, 0, dados.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }
        }
    }
}
