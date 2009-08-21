using System;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en")]

namespace KurtsNetSend
{
  public partial class ReceivedMessages : Form
  {
    private FileStream _fs;
    private SafeFileHandle _handle;
    private bool _okToClose;
    private NativeOverlapped _stnOverlap;


    public ReceivedMessages()
    {
      InitializeComponent();
    }

    private void ReceivedMessages_Resize(object sender, EventArgs e)
    {
      if (FormWindowState.Minimized == WindowState)
        Hide();
    }

    private void notifyIcon1_DoubleClick(object sender, EventArgs e)
    {
      Show();
      WindowState = FormWindowState.Normal;
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      _okToClose = true;
      Close();
    }

    private void ReceivedMessages_Shown(object sender, EventArgs e)
    {
      WindowState = FormWindowState.Minimized;
      Hide();
    }

    private void ReceivedMessages_Load(object sender, EventArgs e)
    {
      // Startup the Mailslot etc.
      _handle = NativeMethods.CreateMailslot("\\\\.\\mailslot\\Messngr", // Messngr
                                             0,
                                             uint.MaxValue,
                                             IntPtr.Zero);
      timer1.Enabled = true;
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
      // Check the mailslot here.
      int nextMsgSize = 0;
      bool ok = NativeMethods.GetMailslotInfo(_handle, IntPtr.Zero, ref nextMsgSize, IntPtr.Zero, IntPtr.Zero);
      if (!ok || nextMsgSize < 1)
        return;
      if (_fs == null)
        _fs = new FileStream(_handle, FileAccess.Read);
      var buffer = new byte[nextMsgSize];
      int chars = _fs.Read(buffer, 0, nextMsgSize);
      var charsArray = new char[Encoding.Default.GetCharCount(buffer)];
      Encoding.Default.GetChars(buffer, 0, chars, charsArray, 0);
      var contents = new string(charsArray);
      string[] contentsArray = contents.Split(new[] {'\0'});
      var li =
        new ListViewItem(new[]
                           {
                             DateTime.Now.ToShortTimeString(), contentsArray[0], contentsArray[1],
                             contentsArray[2]
                           });
      listView1.Items.Add(li);
      Show();
      WindowState = FormWindowState.Normal;
      Console.Beep();
    }

    private void ReceivedMessages_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (!_okToClose)
      {
        WindowState = FormWindowState.Minimized;
        Hide();
        e.Cancel = true;
      }
    }

    private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
    {
      var dlg = new SendDlg();
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        if (!SendMessage(dlg.From, dlg.To, dlg.Message))
          MessageBox.Show("Send Failed", "Kurt's NetSend");
      }
    }

    private bool SendMessage(string from, string to, string message)
    {
      byte[] fromBytes = Encoding.Default.GetBytes(from);
      byte[] toBytes = Encoding.Default.GetBytes(to);
      byte[] messageBytes = Encoding.Default.GetBytes(message);
      int totalCount = fromBytes.Length + toBytes.Length + messageBytes.Length + 3; // 3 null bytes.
      var totalBytes = new byte[totalCount];
      fromBytes.CopyTo(totalBytes, 0);
      totalBytes[fromBytes.Length] = 0;
      toBytes.CopyTo(totalBytes, fromBytes.Length + 1);
      totalBytes[fromBytes.Length + toBytes.Length + 1] = 0;
      messageBytes.CopyTo(totalBytes, fromBytes.Length + toBytes.Length + 2);
      totalBytes[totalCount - 1] = 0;
      SafeFileHandle writeHandle = NativeMethods.CreateFile(String.Format("\\\\{0}\\mailslot\\Messngr", to),
                                                            FileAccess.Write, FileShare.Read, 0, FileMode.Open, 0,
                                                            IntPtr.Zero);
      uint bytesWritten;
      return NativeMethods.WriteFile(writeHandle, totalBytes, (uint) totalCount, out bytesWritten, ref _stnOverlap);
    }
  }

  internal static class NativeMethods
  {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern SafeFileHandle CreateMailslot(string lpName,
                                                         uint nMaxMessageSize,
                                                         uint lReadTimeout,
                                                         IntPtr lpSecurityAttributes);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMailslotInfo(SafeFileHandle hMailslot,
                                                IntPtr lpMaxMessageSize,
                                                ref int lpNextSize,
                                                IntPtr lpMessageCount,
                                                IntPtr lpReadTimeout);

    [DllImport("Kernel32.dll", SetLastError = true,
      CharSet = CharSet.Auto)]
    internal static extern SafeFileHandle CreateFile(
      string fileName,
      [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
      [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
      int securityAttributes,
      [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
      int flags,
      IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool WriteFile(
      SafeFileHandle hFile,
      byte[] lpBuffer,
      uint nNumberOfBytesToWrite,
      out uint lpNumberOfBytesWritten,
      [In] ref NativeOverlapped lpOverlapped);
  }
}