using EI.SI;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace App_servidor
{
    internal class Program
    {
        private const int PORT = 10000;
        private const int NUMBER_OF_ITERATIONS = 50000;
        private static List<Utilizador> listaUtilizadores = new List<Utilizador>();
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);
            TcpListener listener = new TcpListener(endpoint);

            listener.Start();
            Console.WriteLine("Servidor iniciado...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Console.WriteLine($"Um utilizador está a tentar iniciar sessão...");

                ClientHandler clientHandler = new ClientHandler(client);
                clientHandler.Handle();
            }
        }

        public static void Broadcast(string mensagem, string utilizador, bool info)//funçao que envia as mensagens para os utlizadores
        {
            foreach (Utilizador Item in listaUtilizadores)
            {
                if (Item.utilizador != utilizador)//não envia ao mesmo utilizador que enviou
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.cliente;

                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    ProtocolSI protocolSI = new ProtocolSI();
                    Byte[] broadcastBytes;

                    JArray array = new JArray();//cria o array para ser enviado ao utilizador
                    array.Add(utilizador);
                    array.Add(mensagem);
                    array.Add(info);

                    broadcastBytes = protocolSI.Make(ProtocolSICmdType.DATA, cifrarDados(array.ToString(), Item.aes));
                    broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);

                    broadcastStream.Flush();
                }   
            }
        }

        public static void whoOnline(string utilizador, TcpClient broadcastSocket, AesCryptoServiceProvider aes)
        {
            foreach (Utilizador Item in listaUtilizadores)
            {
                if (Item.utilizador != utilizador)//não envia ao mesmo utilizador que enviou
                {
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    ProtocolSI protocolSI = new ProtocolSI();
                    Byte[] broadcastBytes;

                    JArray array = new JArray();//cria o array para ser enviado ao utilizador
                    array.Add(utilizador);
                    array.Add($"{Item.utilizador} está online!");
                    array.Add(true);

                    broadcastBytes = protocolSI.Make(ProtocolSICmdType.DATA, cifrarDados(array.ToString(), aes));
                    broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);

                    broadcastStream.Flush();
                }
            }
        }

        public static bool Register(string username, byte[] saltedPasswordHash, byte[] salt)
        {
            MySqlConnection conn = null;
            try
            {

                // Configurar ligação à Base de Dados
                string connetionString = "server=localhost;database=test;uid=root;";
                conn = new MySqlConnection(connetionString);

                // Abrir ligação à Base de Dados
                conn.Open();

                MySqlCommand mySqlCommand = new MySqlCommand();
                String sql = "SELECT * FROM Users WHERE username = @username";
                mySqlCommand.CommandText = sql;
                MySqlParameter param = new MySqlParameter("@username", username);
                mySqlCommand.Parameters.Add(param);
                mySqlCommand.Connection = conn;

                // Executar comando SQL
                MySqlDataReader reader = mySqlCommand.ExecuteReader();
                if (reader.HasRows)
                {
                    //utilizador encontrado
                    return false;
                }
                reader.Close();

                // Declaração dos parâmetros do comando SQL
                MySqlParameter paramUsername = new MySqlParameter("@username", username);
                MySqlParameter paramPassHash = new MySqlParameter("@saltedPasswordHash", saltedPasswordHash);
                MySqlParameter paramSalt = new MySqlParameter("@salt", salt);

                // Declaração do comando SQL
                sql = "INSERT INTO users (username, pass, salt) VALUES (@username,@saltedPasswordHash,@salt)";

                // Prepara comando SQL para ser executado na Base de Dados
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                // Introduzir valores aos parâmentros registados no comando SQL
                cmd.Parameters.Add(paramUsername);
                cmd.Parameters.Add(paramPassHash);
                cmd.Parameters.Add(paramSalt);

                // Executar comando SQL
                int lines = cmd.ExecuteNonQuery();

                // Fechar ligação
                conn.Close();
                if (lines == 0)
                {
                    // Se forem devolvidas 0 linhas alteradas então o não foi executado com sucesso
                    Console.WriteLine("Erro ao inserir novo cliente");
                    return false;
                }

                Console.WriteLine("Novo cliente adicionado!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while inserting an user:" + e.Message);
                return false;
            }
        }

        private static bool lookForUser(string  user)
        {
            foreach (Utilizador item in listaUtilizadores)
            {
                if (item.utilizador == user)
                    return true;
            }
            return false;
        }

        public static int VerifyLogin(string username, string password, TcpClient client, AesCryptoServiceProvider aes)
        {
            MySqlConnection cnn;
            string connetionString = "server=localhost;database=test;uid=root;";
            cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                MySqlCommand mySqlCommand = new MySqlCommand();
                String sql = "SELECT * FROM Users WHERE username = @username";
                mySqlCommand.CommandText = sql;
                MySqlParameter param = new MySqlParameter("@username", username);
                mySqlCommand.Parameters.Add(param);
                mySqlCommand.Connection = cnn;

                // Executar comando SQL
                MySqlDataReader reader = mySqlCommand.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("No user found");
                    //nenhum utilizador encontrado
                    return -1;
                }
                else
                {
                    reader.Read();

                    // Obter Hash (password + salt)
                    byte[] saltedPasswordHashStored = (byte[])reader["pass"];

                    // Obter salt
                    byte[] saltStored = (byte[])reader["salt"];

                    reader.Close();
                    cnn.Close();
                    byte[] hash = GenerateSaltedHash(password, saltStored);

                    if(saltedPasswordHashStored.SequenceEqual(hash))
                    {
                        Utilizador user = new Utilizador { utilizador = username, cliente = client, aes = aes };

                        if(lookForUser(username))
                            return 2;//utilizador online

                        listaUtilizadores.Add(user);
                        return 1;//login correcto
                    }
                    else
                        return 0;//password incorrecta
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no acesso à base de dados! " + ex.Message);
                return -2;
            }
            
        }
        public static byte[] GenerateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }

        public static byte[] GenerateSaltedHash(string plainText, byte[] salt)
        {
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(plainText, salt, NUMBER_OF_ITERATIONS);
            return rfc2898.GetBytes(32);
        }

        public static string decifrar(byte[] txtCifrado, AesCryptoServiceProvider aes)
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

        public static byte[] cifrarDados(string txt, AesCryptoServiceProvider aes)
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

        public static void removeUser(string user)
        {
            foreach (var item in listaUtilizadores)
            {
                if(item.utilizador == user)
                {
                    listaUtilizadores.Remove(item);
                    return;
                }  
            }
        }

        public static bool verifyData(JArray array, string key)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            rsa.FromXmlString(key);

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] signature = Convert.FromBase64String((string)array[1]);
                byte[] dados = Encoding.UTF8.GetBytes((string)array[0]);

                return rsa.VerifyData(dados, sha1, signature);
            }
        }
    }

    class ClientHandler
    {
        private TcpClient client;
        private JArray utilizador, msgArray;
        string user;
        private const int SALTSIZE = 8;
        AesCryptoServiceProvider aes;
        RSACryptoServiceProvider rsa;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Handle()
        {
            Thread thread = new Thread(ThreadHandler);
            thread.Start();
        }

        public void ThreadHandler()
        {
            NetworkStream networkStream = this.client.GetStream();
            ProtocolSI protocolSI = new ProtocolSI();
            aes = new AesCryptoServiceProvider();
            rsa = new RSACryptoServiceProvider();
            string publickey = rsa.ToXmlString(false); // Chave Pública
            string bothkeys = rsa.ToXmlString(true); // Chave Privada + Pública
            string keySignature = null;
            byte[] dados;
            string mensagem;

            dados = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publickey);
            networkStream.Write(dados, 0, dados.Length);
            while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            }

            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            dados = rsa.Decrypt(protocolSI.GetData(), true);
            aes.Key = GerarChavePrivada(dados);
            aes.IV = GerarIv(dados);
            dados = protocolSI.Make(ProtocolSICmdType.ACK);
            networkStream.Write(dados, 0, dados.Length);
            Console.WriteLine("Key de novo cliente recebida!");

            while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                switch (protocolSI.GetCmdType())
                {
                    case ProtocolSICmdType.PUBLIC_KEY:
                        keySignature = Program.decifrar(protocolSI.GetData(), aes);
                        dados = protocolSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(dados, 0, dados.Length);
                        break;
                    case ProtocolSICmdType.DATA://utilizador esta a enviar mensagem
                        mensagem = Program.decifrar(protocolSI.GetData(), aes);
                        msgArray = JsonConvert.DeserializeObject<JArray>(mensagem);
                        if(!Program.verifyData(msgArray, keySignature))
                        {
                            Console.WriteLine($"Verificação da assinatura de {user} incorrecta!");
                            break;
                        }
                        Console.WriteLine($"{user}:{msgArray[0]}");
                        Program.Broadcast((string)msgArray[0], user, false);
                        break;
                    case ProtocolSICmdType.USER_OPTION_9:
                        Program.whoOnline(user, client, aes);
                        dados = protocolSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(dados, 0, dados.Length);
                        break;
                    case ProtocolSICmdType.USER_OPTION_1://utilizador esta a fazer login
                        mensagem = Program.decifrar(protocolSI.GetData(), aes);
                        utilizador = JsonConvert.DeserializeObject<JArray>(mensagem);
                        user = (string)utilizador[0];
                        int login = Program.VerifyLogin(user, (string)utilizador[1], client, aes);
                        switch (login)
                        {
                            case 0:
                                dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2);//login incorreto
                                break;
                            case 1:
                                dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1);//login correcto
                                Program.Broadcast($"O {user} entrou no chat!", user, true);
                                break;
                            case 2:
                                dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_4);//user online
                                break;
                            case -1:
                                dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_5);//utilizador nao se encontra no sistema
                                break;
                            default:
                                dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_6);//outro erro
                                break;
                        } 
                        networkStream.Write(dados, 0, dados.Length);
                        break;
                    case ProtocolSICmdType.USER_OPTION_3://utilizador esta a registar
                        mensagem = Program.decifrar(protocolSI.GetData(), aes);
                        utilizador = JsonConvert.DeserializeObject<JArray>(mensagem);
                        user = (string)utilizador[0];
                        byte[] salt = Program.GenerateSalt(SALTSIZE);
                        if (Program.Register(user, Program.GenerateSaltedHash((string)utilizador[1], salt), salt))
                            dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1);//Registar correcto
                        else
                            dados = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2);//Registar incorreto
                        networkStream.Write(dados, 0, dados.Length);
                        break;
                    case ProtocolSICmdType.EOT:
                        if (utilizador != null)
                        {
                            Console.WriteLine($"A terminar a thread de {user}...");
                            Program.removeUser(user);
                        }
                        else
                            Console.WriteLine("A terminar a thread de utilizador sem sessão...");
                        break;
                }
            }
            networkStream.Close();
            client.Close();
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

    public class Utilizador
    {
        public string utilizador { get; set; }
        public TcpClient cliente { get; set; }
        public AesCryptoServiceProvider aes { get; set; }
    }
}
