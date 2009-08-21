using System;
using System.Windows.Forms;

namespace KurtsNetSend
{
  public partial class SendDlg : Form
  {
    public SendDlg()
    {
      InitializeComponent();
    }

    public string From
    {
      get { return fromTextBox.Text; }
    }

    public string To
    {
      get { return toTextBox.Text; }
    }

    public string Message
    {
      get { return messageTextBox.Text; }
    }

    private void SendDlg_Load(object sender, EventArgs e)
    {
      fromTextBox.Text = Environment.MachineName;
      ActiveControl = messageTextBox;
    }
  }
}