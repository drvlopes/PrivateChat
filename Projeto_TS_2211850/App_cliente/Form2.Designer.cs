namespace App_cliente
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tb_mensagem = new System.Windows.Forms.TextBox();
            this.bt_enviar = new System.Windows.Forms.Button();
            this.tv_historico = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // tb_mensagem
            // 
            this.tb_mensagem.Location = new System.Drawing.Point(12, 382);
            this.tb_mensagem.MaxLength = 100;
            this.tb_mensagem.Name = "tb_mensagem";
            this.tb_mensagem.Size = new System.Drawing.Size(318, 20);
            this.tb_mensagem.TabIndex = 1;
            this.tb_mensagem.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tb_mensagem_KeyDown);
            // 
            // bt_enviar
            // 
            this.bt_enviar.Location = new System.Drawing.Point(336, 382);
            this.bt_enviar.Name = "bt_enviar";
            this.bt_enviar.Size = new System.Drawing.Size(100, 20);
            this.bt_enviar.TabIndex = 2;
            this.bt_enviar.Text = "Enviar";
            this.bt_enviar.UseVisualStyleBackColor = true;
            this.bt_enviar.Click += new System.EventHandler(this.bt_enviar_Click);
            // 
            // tv_historico
            // 
            this.tv_historico.Location = new System.Drawing.Point(12, 12);
            this.tv_historico.Name = "tv_historico";
            this.tv_historico.ShowLines = false;
            this.tv_historico.ShowPlusMinus = false;
            this.tv_historico.Size = new System.Drawing.Size(424, 364);
            this.tv_historico.TabIndex = 3;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(448, 414);
            this.Controls.Add(this.tv_historico);
            this.Controls.Add(this.bt_enviar);
            this.Controls.Add(this.tb_mensagem);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.Name = "Form2";
            this.Text = "Chat";
            this.Shown += new System.EventHandler(this.Form2_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox tb_mensagem;
        private System.Windows.Forms.Button bt_enviar;
        private System.Windows.Forms.TreeView tv_historico;
    }
}