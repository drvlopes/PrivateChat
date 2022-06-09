using EI.SI;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace App_cliente
{
    public partial class Form1 : Form
    {
        private const int PORT = 10000;
        IPEndPoint endPoint;
        NetworkStream networkStream;
        ProtocolSI protocolSI;
        TcpClient client;
        byte[] dados, passAes;
        RSACryptoServiceProvider rsa;
        AesCryptoServiceProvider aes;
        string passRSA;

        public Form1()
        {
            InitializeComponent();
            try
            {
                endPoint = new IPEndPoint(IPAddress.Loopback, PORT);
                client = new TcpClient();
                client.Connect(endPoint);
                networkStream = client.GetStream();
                protocolSI = new ProtocolSI();
                rsa = new RSACryptoServiceProvider();
                aes = new AesCryptoServiceProvider();
                passAes = GenerateSalt();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possivel estabelecer ligação com o servidor!");
                Application.Exit();
            }

            aes.Key = GerarChavePrivada(passAes);
            aes.IV = GerarIv(passAes);

            //receber chave publica(rsa)
            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            passRSA = protocolSI.GetStringFromData();
            rsa.FromXmlString(passRSA);
            dados = protocolSI.Make(ProtocolSICmdType.ACK);
            networkStream.Write(dados, 0, dados.Length);
            //enviar palavra secreta(aes)
            dados = protocolSI.Make(ProtocolSICmdType.SECRET_KEY, rsa.Encrypt(passAes, true));
            networkStream.Write(dados, 0, dados.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }
        }

        private void bt_login_Click(object sender, EventArgs e)
        {
            try
            {
                string username = tb_username.Text.Trim();
                string password = tb_password.Text.Trim();
                if (username == "" || password == "")  //Apos o .Trim() confirma se está alguma coisa escrita nas textboxs
                {
                    MessageBox.Show("Preencha todos os campos!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                JArray array = new JArray();
                array.Add(username);
                array.Add(password); //criação de um array com o utilizador e password para facilitar a leitura do lado do servidor

                string msg = array.ToString();

                tb_password.Clear();

                byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, cifrarDados(msg));
                networkStream.Write(packet, 0, packet.Length);

                while (protocolSI.GetCmdType() != ProtocolSICmdType.USER_OPTION_1 || protocolSI.GetCmdType() != ProtocolSICmdType.USER_OPTION_2)//app cliente aguarda pela confirmação do servidor se os dados de acesso estávam corretos ou nao
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                    switch (protocolSI.GetCmdType())
                    {
                        case ProtocolSICmdType.USER_OPTION_1:
                            Form2 frm2 = new Form2(client, username, passAes); //Caso positivo é aberto o form de chat global, escondido o de login e ao ser fechado o de cliente que este tmb o seja (podera ser alterado posteriormente)
                            this.Hide();
                            frm2.Closed += (s, args) => this.Close();
                            frm2.Show();
                            return;
                        case ProtocolSICmdType.USER_OPTION_2://Caso negativo é apresentado uma mensagem de erro 
                            MessageBox.Show("Utilizador/Password incorrecta!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        case ProtocolSICmdType.USER_OPTION_4:
                            MessageBox.Show("Utilizador já se encontra online!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        case ProtocolSICmdType.USER_OPTION_5:
                            MessageBox.Show("Utiliador não se encontra no sistema!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        case ProtocolSICmdType.USER_OPTION_6:
                            MessageBox.Show("Erro no servidor!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                    }
                }
            }
            catch(Exception ae)
            {
                MessageBox.Show("Falha na ligação com o servidor!");
            }
        }

        private void CloseCliente() // funcao para garantir que ao fechar a aplicação seja fechadas todas as ligaçoes activas
        {
            byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
            try
            {
                networkStream.Write(eot, 0, eot.Length);
            }
            catch(Exception e)
            {
                
            }
            
            networkStream.Close();
            client.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCliente();//evento que chama a função 
        }

        private void tb_username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) 
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                tb_password.Focus();
            }
        }

        private void tb_password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                bt_login.Focus();
            }  
        }

        private void bt_registar_Click(object sender, EventArgs e)
        {
            try
            {
                string username = tb_username.Text.Trim();
                string password = tb_password.Text.Trim();
                if (username == "" || password == "")  //Apos o .Trim() confirma se está alguma coisa escrita nas textboxs
                {
                    return;
                }

                JArray array = new JArray();
                array.Add(username);
                array.Add(password); //criação de um array com o utilizador e password para facilitar a leitura do lado do servidor

                string msg = array.ToString();

                tb_password.Clear();

                byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_3, cifrarDados(msg));
                networkStream.Write(packet, 0, packet.Length);

                while (protocolSI.GetCmdType() != ProtocolSICmdType.USER_OPTION_1 || protocolSI.GetCmdType() != ProtocolSICmdType.USER_OPTION_2)//app cliente aguarda pela confirmação do servidor se os dados de acesso estávam corretos ou nao
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                    switch (protocolSI.GetCmdType())
                    {
                        case ProtocolSICmdType.USER_OPTION_1:
                            MessageBox.Show("Utilizador criado com sucesso!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        case ProtocolSICmdType.USER_OPTION_2://Caso negativo é apresentado uma mensagem de erro 
                            MessageBox.Show("Erro na criação do utilizador!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Falha na ligação com o servidor!");
            }
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

        private static byte[] GenerateSalt()
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[8];
            rng.GetBytes(buff);
            //return buff.ToString();
            return buff;
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
    }
}

