Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class PolicyEditorForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PolicyEditorForm))
        Me.PolicyPanel = New System.Windows.Forms.Panel()
        Me.CustomIdentifiersConfigureButton = New System.Windows.Forms.Button()
        Me.CustomIdentifiersCheckBox = New System.Windows.Forms.CheckBox()
        Me.CustomDictionariesConfigureButton = New System.Windows.Forms.Button()
        Me.CustomDictionariesCheckBox = New System.Windows.Forms.CheckBox()
        Me.ZipCodesConfigureButton = New System.Windows.Forms.Button()
        Me.ZipCodesCheckBox = New System.Windows.Forms.CheckBox()
        Me.VINsConfigureButton = New System.Windows.Forms.Button()
        Me.VINsCheckBox = New System.Windows.Forms.CheckBox()
        Me.URLsConfigureButton = New System.Windows.Forms.Button()
        Me.URLsCheckBox = New System.Windows.Forms.CheckBox()
        Me.SurnamesConfigureButton = New System.Windows.Forms.Button()
        Me.SurnamesCheckBox = New System.Windows.Forms.CheckBox()
        Me.StateAbbreviationsConfigureButton = New System.Windows.Forms.Button()
        Me.StateAbbreviationsCheckBox = New System.Windows.Forms.CheckBox()
        Me.StatesConfigureButton = New System.Windows.Forms.Button()
        Me.StatesCheckBox = New System.Windows.Forms.CheckBox()
        Me.SSNsConfigureButton = New System.Windows.Forms.Button()
        Me.SSNsCheckBox = New System.Windows.Forms.CheckBox()
        Me.PhoneNumberExtsConfigureButton = New System.Windows.Forms.Button()
        Me.PhoneNumberExtsCheckBox = New System.Windows.Forms.CheckBox()
        Me.PhoneNumbersConfigureButton = New System.Windows.Forms.Button()
        Me.PhoneNumbersCheckBox = New System.Windows.Forms.CheckBox()
        Me.NERConfigureButton = New System.Windows.Forms.Button()
        Me.NERCheckBox = New System.Windows.Forms.CheckBox()
        Me.IPAddressesConfigureButton = New System.Windows.Forms.Button()
        Me.IPAddressesCheckBox = New System.Windows.Forms.CheckBox()
        Me.HospitalsConfigureButton = New System.Windows.Forms.Button()
        Me.HospitalsCheckBox = New System.Windows.Forms.CheckBox()
        Me.HospitalAbbreviationsConfigureButton = New System.Windows.Forms.Button()
        Me.HospitalAbbreviationsCheckBox = New System.Windows.Forms.CheckBox()
        Me.FirstNamesConfigureButton = New System.Windows.Forms.Button()
        Me.FirstNamesCheckBox = New System.Windows.Forms.CheckBox()
        Me.EmailAddressesConfigureButton = New System.Windows.Forms.Button()
        Me.EmailAddressesCheckBox = New System.Windows.Forms.CheckBox()
        Me.DatesConfigureButton = New System.Windows.Forms.Button()
        Me.DatesCheckBox = New System.Windows.Forms.CheckBox()
        Me.CreditCardsConfigureButton = New System.Windows.Forms.Button()
        Me.CreditCardsCheckBox = New System.Windows.Forms.CheckBox()
        Me.CountiesConfigureButton = New System.Windows.Forms.Button()
        Me.CountiesCheckBox = New System.Windows.Forms.CheckBox()
        Me.CitiesConfigureButton = New System.Windows.Forms.Button()
        Me.CitiesCheckBox = New System.Windows.Forms.CheckBox()
        Me.AgesConfigureButton = New System.Windows.Forms.Button()
        Me.AgesCheckBox = New System.Windows.Forms.CheckBox()
        Me.PolicysToolStrip = New System.Windows.Forms.ToolStrip()
        Me.ToolStripLabel1 = New System.Windows.Forms.ToolStripLabel()
        Me.PolicysToolStripDropDownButton = New System.Windows.Forms.ToolStripComboBox()
        Me.RefreshToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator6 = New System.Windows.Forms.ToolStripSeparator()
        Me.NewToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.SaveToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.SaveAsToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.DeleteToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.PolicyPanel.SuspendLayout()
        Me.PolicysToolStrip.SuspendLayout()
        Me.SuspendLayout()
        '
        'PolicyPanel
        '
        Me.PolicyPanel.Controls.Add(Me.CustomIdentifiersConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.CustomIdentifiersCheckBox)
        Me.PolicyPanel.Controls.Add(Me.CustomDictionariesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.CustomDictionariesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.ZipCodesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.ZipCodesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.VINsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.VINsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.URLsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.URLsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.SurnamesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.SurnamesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.StateAbbreviationsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.StateAbbreviationsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.StatesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.StatesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.SSNsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.SSNsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.PhoneNumberExtsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.PhoneNumberExtsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.PhoneNumbersConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.PhoneNumbersCheckBox)
        Me.PolicyPanel.Controls.Add(Me.NERConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.NERCheckBox)
        Me.PolicyPanel.Controls.Add(Me.IPAddressesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.IPAddressesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.HospitalsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.HospitalsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.HospitalAbbreviationsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.HospitalAbbreviationsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.FirstNamesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.FirstNamesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.EmailAddressesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.EmailAddressesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.DatesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.DatesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.CreditCardsConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.CreditCardsCheckBox)
        Me.PolicyPanel.Controls.Add(Me.CountiesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.CountiesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.CitiesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.CitiesCheckBox)
        Me.PolicyPanel.Controls.Add(Me.AgesConfigureButton)
        Me.PolicyPanel.Controls.Add(Me.AgesCheckBox)
        Me.PolicyPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PolicyPanel.Enabled = False
        Me.PolicyPanel.Location = New System.Drawing.Point(0, 34)
        Me.PolicyPanel.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.PolicyPanel.Name = "PolicyPanel"
        Me.PolicyPanel.Size = New System.Drawing.Size(1381, 509)
        Me.PolicyPanel.TabIndex = 10
        '
        'CustomIdentifiersConfigureButton
        '
        Me.CustomIdentifiersConfigureButton.Enabled = False
        Me.CustomIdentifiersConfigureButton.Location = New System.Drawing.Point(1189, 77)
        Me.CustomIdentifiersConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CustomIdentifiersConfigureButton.Name = "CustomIdentifiersConfigureButton"
        Me.CustomIdentifiersConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.CustomIdentifiersConfigureButton.TabIndex = 45
        Me.CustomIdentifiersConfigureButton.Text = "Configure..."
        Me.CustomIdentifiersConfigureButton.UseVisualStyleBackColor = True
        '
        'CustomIdentifiersCheckBox
        '
        Me.CustomIdentifiersCheckBox.AutoSize = True
        Me.CustomIdentifiersCheckBox.Location = New System.Drawing.Point(984, 83)
        Me.CustomIdentifiersCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CustomIdentifiersCheckBox.Name = "CustomIdentifiersCheckBox"
        Me.CustomIdentifiersCheckBox.Size = New System.Drawing.Size(164, 24)
        Me.CustomIdentifiersCheckBox.TabIndex = 44
        Me.CustomIdentifiersCheckBox.Text = "Custom Identifiers"
        Me.CustomIdentifiersCheckBox.UseVisualStyleBackColor = True
        '
        'CustomDictionariesConfigureButton
        '
        Me.CustomDictionariesConfigureButton.Enabled = False
        Me.CustomDictionariesConfigureButton.Location = New System.Drawing.Point(1189, 32)
        Me.CustomDictionariesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CustomDictionariesConfigureButton.Name = "CustomDictionariesConfigureButton"
        Me.CustomDictionariesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.CustomDictionariesConfigureButton.TabIndex = 43
        Me.CustomDictionariesConfigureButton.Text = "Configure..."
        Me.CustomDictionariesConfigureButton.UseVisualStyleBackColor = True
        '
        'CustomDictionariesCheckBox
        '
        Me.CustomDictionariesCheckBox.AutoSize = True
        Me.CustomDictionariesCheckBox.Location = New System.Drawing.Point(984, 39)
        Me.CustomDictionariesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CustomDictionariesCheckBox.Name = "CustomDictionariesCheckBox"
        Me.CustomDictionariesCheckBox.Size = New System.Drawing.Size(177, 24)
        Me.CustomDictionariesCheckBox.TabIndex = 42
        Me.CustomDictionariesCheckBox.Text = "Custom Dictionaries"
        Me.CustomDictionariesCheckBox.UseVisualStyleBackColor = True
        '
        'ZipCodesConfigureButton
        '
        Me.ZipCodesConfigureButton.Enabled = False
        Me.ZipCodesConfigureButton.Location = New System.Drawing.Point(712, 437)
        Me.ZipCodesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.ZipCodesConfigureButton.Name = "ZipCodesConfigureButton"
        Me.ZipCodesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.ZipCodesConfigureButton.TabIndex = 41
        Me.ZipCodesConfigureButton.Text = "Configure..."
        Me.ZipCodesConfigureButton.UseVisualStyleBackColor = True
        '
        'ZipCodesCheckBox
        '
        Me.ZipCodesCheckBox.AutoSize = True
        Me.ZipCodesCheckBox.Location = New System.Drawing.Point(507, 443)
        Me.ZipCodesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.ZipCodesCheckBox.Name = "ZipCodesCheckBox"
        Me.ZipCodesCheckBox.Size = New System.Drawing.Size(107, 24)
        Me.ZipCodesCheckBox.TabIndex = 40
        Me.ZipCodesCheckBox.Text = "Zip Codes"
        Me.ZipCodesCheckBox.UseVisualStyleBackColor = True
        '
        'VINsConfigureButton
        '
        Me.VINsConfigureButton.Enabled = False
        Me.VINsConfigureButton.Location = New System.Drawing.Point(712, 392)
        Me.VINsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.VINsConfigureButton.Name = "VINsConfigureButton"
        Me.VINsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.VINsConfigureButton.TabIndex = 39
        Me.VINsConfigureButton.Text = "Configure..."
        Me.VINsConfigureButton.UseVisualStyleBackColor = True
        '
        'VINsCheckBox
        '
        Me.VINsCheckBox.AutoSize = True
        Me.VINsCheckBox.Location = New System.Drawing.Point(507, 399)
        Me.VINsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.VINsCheckBox.Name = "VINsCheckBox"
        Me.VINsCheckBox.Size = New System.Drawing.Size(70, 24)
        Me.VINsCheckBox.TabIndex = 38
        Me.VINsCheckBox.Text = "VINs"
        Me.VINsCheckBox.UseVisualStyleBackColor = True
        '
        'URLsConfigureButton
        '
        Me.URLsConfigureButton.Enabled = False
        Me.URLsConfigureButton.Location = New System.Drawing.Point(712, 348)
        Me.URLsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.URLsConfigureButton.Name = "URLsConfigureButton"
        Me.URLsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.URLsConfigureButton.TabIndex = 37
        Me.URLsConfigureButton.Text = "Configure..."
        Me.URLsConfigureButton.UseVisualStyleBackColor = True
        '
        'URLsCheckBox
        '
        Me.URLsCheckBox.AutoSize = True
        Me.URLsCheckBox.Location = New System.Drawing.Point(507, 353)
        Me.URLsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.URLsCheckBox.Name = "URLsCheckBox"
        Me.URLsCheckBox.Size = New System.Drawing.Size(76, 24)
        Me.URLsCheckBox.TabIndex = 36
        Me.URLsCheckBox.Text = "URLs"
        Me.URLsCheckBox.UseVisualStyleBackColor = True
        '
        'SurnamesConfigureButton
        '
        Me.SurnamesConfigureButton.Enabled = False
        Me.SurnamesConfigureButton.Location = New System.Drawing.Point(712, 303)
        Me.SurnamesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.SurnamesConfigureButton.Name = "SurnamesConfigureButton"
        Me.SurnamesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.SurnamesConfigureButton.TabIndex = 35
        Me.SurnamesConfigureButton.Text = "Configure..."
        Me.SurnamesConfigureButton.UseVisualStyleBackColor = True
        '
        'SurnamesCheckBox
        '
        Me.SurnamesCheckBox.AutoSize = True
        Me.SurnamesCheckBox.Location = New System.Drawing.Point(507, 309)
        Me.SurnamesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.SurnamesCheckBox.Name = "SurnamesCheckBox"
        Me.SurnamesCheckBox.Size = New System.Drawing.Size(108, 24)
        Me.SurnamesCheckBox.TabIndex = 34
        Me.SurnamesCheckBox.Text = "Surnames"
        Me.SurnamesCheckBox.UseVisualStyleBackColor = True
        '
        'StateAbbreviationsConfigureButton
        '
        Me.StateAbbreviationsConfigureButton.Enabled = False
        Me.StateAbbreviationsConfigureButton.Location = New System.Drawing.Point(712, 259)
        Me.StateAbbreviationsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.StateAbbreviationsConfigureButton.Name = "StateAbbreviationsConfigureButton"
        Me.StateAbbreviationsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.StateAbbreviationsConfigureButton.TabIndex = 33
        Me.StateAbbreviationsConfigureButton.Text = "Configure..."
        Me.StateAbbreviationsConfigureButton.UseVisualStyleBackColor = True
        '
        'StateAbbreviationsCheckBox
        '
        Me.StateAbbreviationsCheckBox.AutoSize = True
        Me.StateAbbreviationsCheckBox.Location = New System.Drawing.Point(507, 264)
        Me.StateAbbreviationsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.StateAbbreviationsCheckBox.Name = "StateAbbreviationsCheckBox"
        Me.StateAbbreviationsCheckBox.Size = New System.Drawing.Size(174, 24)
        Me.StateAbbreviationsCheckBox.TabIndex = 32
        Me.StateAbbreviationsCheckBox.Text = "State Abbreviations"
        Me.StateAbbreviationsCheckBox.UseVisualStyleBackColor = True
        '
        'StatesConfigureButton
        '
        Me.StatesConfigureButton.Enabled = False
        Me.StatesConfigureButton.Location = New System.Drawing.Point(712, 213)
        Me.StatesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.StatesConfigureButton.Name = "StatesConfigureButton"
        Me.StatesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.StatesConfigureButton.TabIndex = 31
        Me.StatesConfigureButton.Text = "Configure..."
        Me.StatesConfigureButton.UseVisualStyleBackColor = True
        '
        'StatesCheckBox
        '
        Me.StatesCheckBox.AutoSize = True
        Me.StatesCheckBox.Location = New System.Drawing.Point(507, 220)
        Me.StatesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.StatesCheckBox.Name = "StatesCheckBox"
        Me.StatesCheckBox.Size = New System.Drawing.Size(82, 24)
        Me.StatesCheckBox.TabIndex = 30
        Me.StatesCheckBox.Text = "States"
        Me.StatesCheckBox.UseVisualStyleBackColor = True
        '
        'SSNsConfigureButton
        '
        Me.SSNsConfigureButton.Enabled = False
        Me.SSNsConfigureButton.Location = New System.Drawing.Point(712, 169)
        Me.SSNsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.SSNsConfigureButton.Name = "SSNsConfigureButton"
        Me.SSNsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.SSNsConfigureButton.TabIndex = 29
        Me.SSNsConfigureButton.Text = "Configure..."
        Me.SSNsConfigureButton.UseVisualStyleBackColor = True
        '
        'SSNsCheckBox
        '
        Me.SSNsCheckBox.AutoSize = True
        Me.SSNsCheckBox.Location = New System.Drawing.Point(507, 176)
        Me.SSNsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.SSNsCheckBox.Name = "SSNsCheckBox"
        Me.SSNsCheckBox.Size = New System.Drawing.Size(76, 24)
        Me.SSNsCheckBox.TabIndex = 28
        Me.SSNsCheckBox.Text = "SSNs"
        Me.SSNsCheckBox.UseVisualStyleBackColor = True
        '
        'PhoneNumberExtsConfigureButton
        '
        Me.PhoneNumberExtsConfigureButton.Enabled = False
        Me.PhoneNumberExtsConfigureButton.Location = New System.Drawing.Point(712, 124)
        Me.PhoneNumberExtsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.PhoneNumberExtsConfigureButton.Name = "PhoneNumberExtsConfigureButton"
        Me.PhoneNumberExtsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.PhoneNumberExtsConfigureButton.TabIndex = 27
        Me.PhoneNumberExtsConfigureButton.Text = "Configure..."
        Me.PhoneNumberExtsConfigureButton.UseVisualStyleBackColor = True
        '
        'PhoneNumberExtsCheckBox
        '
        Me.PhoneNumberExtsCheckBox.AutoSize = True
        Me.PhoneNumberExtsCheckBox.Location = New System.Drawing.Point(507, 131)
        Me.PhoneNumberExtsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.PhoneNumberExtsCheckBox.Name = "PhoneNumberExtsCheckBox"
        Me.PhoneNumberExtsCheckBox.Size = New System.Drawing.Size(180, 24)
        Me.PhoneNumberExtsCheckBox.TabIndex = 26
        Me.PhoneNumberExtsCheckBox.Text = "Phone Number Exts."
        Me.PhoneNumberExtsCheckBox.UseVisualStyleBackColor = True
        '
        'PhoneNumbersConfigureButton
        '
        Me.PhoneNumbersConfigureButton.Enabled = False
        Me.PhoneNumbersConfigureButton.Location = New System.Drawing.Point(712, 80)
        Me.PhoneNumbersConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.PhoneNumbersConfigureButton.Name = "PhoneNumbersConfigureButton"
        Me.PhoneNumbersConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.PhoneNumbersConfigureButton.TabIndex = 25
        Me.PhoneNumbersConfigureButton.Text = "Configure..."
        Me.PhoneNumbersConfigureButton.UseVisualStyleBackColor = True
        '
        'PhoneNumbersCheckBox
        '
        Me.PhoneNumbersCheckBox.AutoSize = True
        Me.PhoneNumbersCheckBox.Location = New System.Drawing.Point(507, 87)
        Me.PhoneNumbersCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.PhoneNumbersCheckBox.Name = "PhoneNumbersCheckBox"
        Me.PhoneNumbersCheckBox.Size = New System.Drawing.Size(149, 24)
        Me.PhoneNumbersCheckBox.TabIndex = 24
        Me.PhoneNumbersCheckBox.Text = "Phone Numbers"
        Me.PhoneNumbersCheckBox.UseVisualStyleBackColor = True
        '
        'NERConfigureButton
        '
        Me.NERConfigureButton.Enabled = False
        Me.NERConfigureButton.Location = New System.Drawing.Point(712, 36)
        Me.NERConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.NERConfigureButton.Name = "NERConfigureButton"
        Me.NERConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.NERConfigureButton.TabIndex = 23
        Me.NERConfigureButton.Text = "Configure..."
        Me.NERConfigureButton.UseVisualStyleBackColor = True
        '
        'NERCheckBox
        '
        Me.NERCheckBox.AutoSize = True
        Me.NERCheckBox.Location = New System.Drawing.Point(507, 41)
        Me.NERCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.NERCheckBox.Name = "NERCheckBox"
        Me.NERCheckBox.Size = New System.Drawing.Size(197, 24)
        Me.NERCheckBox.TabIndex = 22
        Me.NERCheckBox.Text = "NER (English Persons)"
        Me.NERCheckBox.UseVisualStyleBackColor = True
        '
        'IPAddressesConfigureButton
        '
        Me.IPAddressesConfigureButton.Enabled = False
        Me.IPAddressesConfigureButton.Location = New System.Drawing.Point(234, 431)
        Me.IPAddressesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.IPAddressesConfigureButton.Name = "IPAddressesConfigureButton"
        Me.IPAddressesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.IPAddressesConfigureButton.TabIndex = 21
        Me.IPAddressesConfigureButton.Text = "Configure..."
        Me.IPAddressesConfigureButton.UseVisualStyleBackColor = True
        '
        'IPAddressesCheckBox
        '
        Me.IPAddressesCheckBox.AutoSize = True
        Me.IPAddressesCheckBox.Location = New System.Drawing.Point(28, 437)
        Me.IPAddressesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.IPAddressesCheckBox.Name = "IPAddressesCheckBox"
        Me.IPAddressesCheckBox.Size = New System.Drawing.Size(130, 24)
        Me.IPAddressesCheckBox.TabIndex = 20
        Me.IPAddressesCheckBox.Text = "IP Addresses"
        Me.IPAddressesCheckBox.UseVisualStyleBackColor = True
        '
        'HospitalsConfigureButton
        '
        Me.HospitalsConfigureButton.Enabled = False
        Me.HospitalsConfigureButton.Location = New System.Drawing.Point(234, 387)
        Me.HospitalsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.HospitalsConfigureButton.Name = "HospitalsConfigureButton"
        Me.HospitalsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.HospitalsConfigureButton.TabIndex = 17
        Me.HospitalsConfigureButton.Text = "Configure..."
        Me.HospitalsConfigureButton.UseVisualStyleBackColor = True
        '
        'HospitalsCheckBox
        '
        Me.HospitalsCheckBox.AutoSize = True
        Me.HospitalsCheckBox.Location = New System.Drawing.Point(28, 392)
        Me.HospitalsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.HospitalsCheckBox.Name = "HospitalsCheckBox"
        Me.HospitalsCheckBox.Size = New System.Drawing.Size(101, 24)
        Me.HospitalsCheckBox.TabIndex = 16
        Me.HospitalsCheckBox.Text = "Hospitals"
        Me.HospitalsCheckBox.UseVisualStyleBackColor = True
        '
        'HospitalAbbreviationsConfigureButton
        '
        Me.HospitalAbbreviationsConfigureButton.Enabled = False
        Me.HospitalAbbreviationsConfigureButton.Location = New System.Drawing.Point(234, 341)
        Me.HospitalAbbreviationsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.HospitalAbbreviationsConfigureButton.Name = "HospitalAbbreviationsConfigureButton"
        Me.HospitalAbbreviationsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.HospitalAbbreviationsConfigureButton.TabIndex = 15
        Me.HospitalAbbreviationsConfigureButton.Text = "Configure..."
        Me.HospitalAbbreviationsConfigureButton.UseVisualStyleBackColor = True
        '
        'HospitalAbbreviationsCheckBox
        '
        Me.HospitalAbbreviationsCheckBox.AutoSize = True
        Me.HospitalAbbreviationsCheckBox.Location = New System.Drawing.Point(28, 348)
        Me.HospitalAbbreviationsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.HospitalAbbreviationsCheckBox.Name = "HospitalAbbreviationsCheckBox"
        Me.HospitalAbbreviationsCheckBox.Size = New System.Drawing.Size(193, 24)
        Me.HospitalAbbreviationsCheckBox.TabIndex = 14
        Me.HospitalAbbreviationsCheckBox.Text = "Hospital Abbreviations"
        Me.HospitalAbbreviationsCheckBox.UseVisualStyleBackColor = True
        '
        'FirstNamesConfigureButton
        '
        Me.FirstNamesConfigureButton.Enabled = False
        Me.FirstNamesConfigureButton.Location = New System.Drawing.Point(234, 297)
        Me.FirstNamesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.FirstNamesConfigureButton.Name = "FirstNamesConfigureButton"
        Me.FirstNamesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.FirstNamesConfigureButton.TabIndex = 13
        Me.FirstNamesConfigureButton.Text = "Configure..."
        Me.FirstNamesConfigureButton.UseVisualStyleBackColor = True
        '
        'FirstNamesCheckBox
        '
        Me.FirstNamesCheckBox.AutoSize = True
        Me.FirstNamesCheckBox.Location = New System.Drawing.Point(28, 303)
        Me.FirstNamesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.FirstNamesCheckBox.Name = "FirstNamesCheckBox"
        Me.FirstNamesCheckBox.Size = New System.Drawing.Size(120, 24)
        Me.FirstNamesCheckBox.TabIndex = 12
        Me.FirstNamesCheckBox.Text = "First Names"
        Me.FirstNamesCheckBox.UseVisualStyleBackColor = True
        '
        'EmailAddressesConfigureButton
        '
        Me.EmailAddressesConfigureButton.Enabled = False
        Me.EmailAddressesConfigureButton.Location = New System.Drawing.Point(234, 252)
        Me.EmailAddressesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.EmailAddressesConfigureButton.Name = "EmailAddressesConfigureButton"
        Me.EmailAddressesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.EmailAddressesConfigureButton.TabIndex = 11
        Me.EmailAddressesConfigureButton.Text = "Configure..."
        Me.EmailAddressesConfigureButton.UseVisualStyleBackColor = True
        '
        'EmailAddressesCheckBox
        '
        Me.EmailAddressesCheckBox.AutoSize = True
        Me.EmailAddressesCheckBox.Location = New System.Drawing.Point(28, 259)
        Me.EmailAddressesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.EmailAddressesCheckBox.Name = "EmailAddressesCheckBox"
        Me.EmailAddressesCheckBox.Size = New System.Drawing.Size(137, 24)
        Me.EmailAddressesCheckBox.TabIndex = 10
        Me.EmailAddressesCheckBox.Text = "Email Address"
        Me.EmailAddressesCheckBox.UseVisualStyleBackColor = True
        '
        'DatesConfigureButton
        '
        Me.DatesConfigureButton.Enabled = False
        Me.DatesConfigureButton.Location = New System.Drawing.Point(234, 208)
        Me.DatesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.DatesConfigureButton.Name = "DatesConfigureButton"
        Me.DatesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.DatesConfigureButton.TabIndex = 9
        Me.DatesConfigureButton.Text = "Configure..."
        Me.DatesConfigureButton.UseVisualStyleBackColor = True
        '
        'DatesCheckBox
        '
        Me.DatesCheckBox.AutoSize = True
        Me.DatesCheckBox.Location = New System.Drawing.Point(28, 213)
        Me.DatesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.DatesCheckBox.Name = "DatesCheckBox"
        Me.DatesCheckBox.Size = New System.Drawing.Size(78, 24)
        Me.DatesCheckBox.TabIndex = 8
        Me.DatesCheckBox.Text = "Dates"
        Me.DatesCheckBox.UseVisualStyleBackColor = True
        '
        'CreditCardsConfigureButton
        '
        Me.CreditCardsConfigureButton.Enabled = False
        Me.CreditCardsConfigureButton.Location = New System.Drawing.Point(234, 163)
        Me.CreditCardsConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CreditCardsConfigureButton.Name = "CreditCardsConfigureButton"
        Me.CreditCardsConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.CreditCardsConfigureButton.TabIndex = 7
        Me.CreditCardsConfigureButton.Text = "Configure..."
        Me.CreditCardsConfigureButton.UseVisualStyleBackColor = True
        '
        'CreditCardsCheckBox
        '
        Me.CreditCardsCheckBox.AutoSize = True
        Me.CreditCardsCheckBox.Location = New System.Drawing.Point(28, 169)
        Me.CreditCardsCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CreditCardsCheckBox.Name = "CreditCardsCheckBox"
        Me.CreditCardsCheckBox.Size = New System.Drawing.Size(123, 24)
        Me.CreditCardsCheckBox.TabIndex = 6
        Me.CreditCardsCheckBox.Text = "Credit Cards"
        Me.CreditCardsCheckBox.UseVisualStyleBackColor = True
        '
        'CountiesConfigureButton
        '
        Me.CountiesConfigureButton.Enabled = False
        Me.CountiesConfigureButton.Location = New System.Drawing.Point(234, 119)
        Me.CountiesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CountiesConfigureButton.Name = "CountiesConfigureButton"
        Me.CountiesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.CountiesConfigureButton.TabIndex = 5
        Me.CountiesConfigureButton.Text = "Configure..."
        Me.CountiesConfigureButton.UseVisualStyleBackColor = True
        '
        'CountiesCheckBox
        '
        Me.CountiesCheckBox.AutoSize = True
        Me.CountiesCheckBox.Location = New System.Drawing.Point(28, 124)
        Me.CountiesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CountiesCheckBox.Name = "CountiesCheckBox"
        Me.CountiesCheckBox.Size = New System.Drawing.Size(98, 24)
        Me.CountiesCheckBox.TabIndex = 4
        Me.CountiesCheckBox.Text = "Counties"
        Me.CountiesCheckBox.UseVisualStyleBackColor = True
        '
        'CitiesConfigureButton
        '
        Me.CitiesConfigureButton.Enabled = False
        Me.CitiesConfigureButton.Location = New System.Drawing.Point(234, 73)
        Me.CitiesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CitiesConfigureButton.Name = "CitiesConfigureButton"
        Me.CitiesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.CitiesConfigureButton.TabIndex = 3
        Me.CitiesConfigureButton.Text = "Configure..."
        Me.CitiesConfigureButton.UseVisualStyleBackColor = True
        '
        'CitiesCheckBox
        '
        Me.CitiesCheckBox.AutoSize = True
        Me.CitiesCheckBox.Location = New System.Drawing.Point(28, 80)
        Me.CitiesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.CitiesCheckBox.Name = "CitiesCheckBox"
        Me.CitiesCheckBox.Size = New System.Drawing.Size(74, 24)
        Me.CitiesCheckBox.TabIndex = 2
        Me.CitiesCheckBox.Text = "Cities"
        Me.CitiesCheckBox.UseVisualStyleBackColor = True
        '
        'AgesConfigureButton
        '
        Me.AgesConfigureButton.Enabled = False
        Me.AgesConfigureButton.Location = New System.Drawing.Point(234, 29)
        Me.AgesConfigureButton.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.AgesConfigureButton.Name = "AgesConfigureButton"
        Me.AgesConfigureButton.Size = New System.Drawing.Size(157, 36)
        Me.AgesConfigureButton.TabIndex = 1
        Me.AgesConfigureButton.Text = "Configure..."
        Me.AgesConfigureButton.UseVisualStyleBackColor = True
        '
        'AgesCheckBox
        '
        Me.AgesCheckBox.AutoSize = True
        Me.AgesCheckBox.Location = New System.Drawing.Point(28, 36)
        Me.AgesCheckBox.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.AgesCheckBox.Name = "AgesCheckBox"
        Me.AgesCheckBox.Size = New System.Drawing.Size(72, 24)
        Me.AgesCheckBox.TabIndex = 0
        Me.AgesCheckBox.Text = "Ages"
        Me.AgesCheckBox.UseVisualStyleBackColor = True
        '
        'PolicysToolStrip
        '
        Me.PolicysToolStrip.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.PolicysToolStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripLabel1, Me.PolicysToolStripDropDownButton, Me.RefreshToolStripButton, Me.ToolStripSeparator6, Me.NewToolStripButton, Me.SaveToolStripButton, Me.SaveAsToolStripButton, Me.DeleteToolStripButton})
        Me.PolicysToolStrip.Location = New System.Drawing.Point(0, 0)
        Me.PolicysToolStrip.Name = "PolicysToolStrip"
        Me.PolicysToolStrip.Padding = New System.Windows.Forms.Padding(0, 0, 3, 0)
        Me.PolicysToolStrip.Size = New System.Drawing.Size(1381, 34)
        Me.PolicysToolStrip.TabIndex = 9
        Me.PolicysToolStrip.Text = "ToolStrip3"
        '
        'ToolStripLabel1
        '
        Me.ToolStripLabel1.Name = "ToolStripLabel1"
        Me.ToolStripLabel1.Size = New System.Drawing.Size(126, 29)
        Me.ToolStripLabel1.Text = "Select a Policy:"
        '
        'PolicysToolStripDropDownButton
        '
        Me.PolicysToolStripDropDownButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.PolicysToolStripDropDownButton.Name = "PolicysToolStripDropDownButton"
        Me.PolicysToolStripDropDownButton.Size = New System.Drawing.Size(410, 34)
        '
        'RefreshToolStripButton
        '
        Me.RefreshToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.RefreshToolStripButton.Image = CType(resources.GetObject("RefreshToolStripButton.Image"), System.Drawing.Image)
        Me.RefreshToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.RefreshToolStripButton.Name = "RefreshToolStripButton"
        Me.RefreshToolStripButton.Size = New System.Drawing.Size(34, 29)
        Me.RefreshToolStripButton.Text = "Refresh"
        '
        'ToolStripSeparator6
        '
        Me.ToolStripSeparator6.Name = "ToolStripSeparator6"
        Me.ToolStripSeparator6.Size = New System.Drawing.Size(6, 34)
        '
        'NewToolStripButton
        '
        Me.NewToolStripButton.Image = CType(resources.GetObject("NewToolStripButton.Image"), System.Drawing.Image)
        Me.NewToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.NewToolStripButton.Name = "NewToolStripButton"
        Me.NewToolStripButton.Size = New System.Drawing.Size(87, 29)
        Me.NewToolStripButton.Text = "New..."
        '
        'SaveToolStripButton
        '
        Me.SaveToolStripButton.Enabled = False
        Me.SaveToolStripButton.Image = CType(resources.GetObject("SaveToolStripButton.Image"), System.Drawing.Image)
        Me.SaveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.SaveToolStripButton.Name = "SaveToolStripButton"
        Me.SaveToolStripButton.Size = New System.Drawing.Size(77, 29)
        Me.SaveToolStripButton.Text = "Save"
        '
        'SaveAsToolStripButton
        '
        Me.SaveAsToolStripButton.Enabled = False
        Me.SaveAsToolStripButton.Image = CType(resources.GetObject("SaveAsToolStripButton.Image"), System.Drawing.Image)
        Me.SaveAsToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.SaveAsToolStripButton.Name = "SaveAsToolStripButton"
        Me.SaveAsToolStripButton.Size = New System.Drawing.Size(114, 29)
        Me.SaveAsToolStripButton.Text = "Save As..."
        '
        'DeleteToolStripButton
        '
        Me.DeleteToolStripButton.Enabled = False
        Me.DeleteToolStripButton.Image = CType(resources.GetObject("DeleteToolStripButton.Image"), System.Drawing.Image)
        Me.DeleteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.DeleteToolStripButton.Name = "DeleteToolStripButton"
        Me.DeleteToolStripButton.Size = New System.Drawing.Size(102, 29)
        Me.DeleteToolStripButton.Text = "Delete..."
        '
        'PolicyEditorForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1381, 543)
        Me.Controls.Add(Me.PolicyPanel)
        Me.Controls.Add(Me.PolicysToolStrip)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.MaximizeBox = False
        Me.MinimumSize = New System.Drawing.Size(1395, 576)
        Me.Name = "PolicyEditorForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Philter Policy Editor"
        Me.PolicyPanel.ResumeLayout(False)
        Me.PolicyPanel.PerformLayout()
        Me.PolicysToolStrip.ResumeLayout(False)
        Me.PolicysToolStrip.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PolicyPanel As Panel
    Friend WithEvents CustomIdentifiersConfigureButton As Button
    Friend WithEvents CustomIdentifiersCheckBox As CheckBox
    Friend WithEvents CustomDictionariesConfigureButton As Button
    Friend WithEvents CustomDictionariesCheckBox As CheckBox
    Friend WithEvents ZipCodesConfigureButton As Button
    Friend WithEvents ZipCodesCheckBox As CheckBox
    Friend WithEvents VINsConfigureButton As Button
    Friend WithEvents VINsCheckBox As CheckBox
    Friend WithEvents URLsConfigureButton As Button
    Friend WithEvents URLsCheckBox As CheckBox
    Friend WithEvents SurnamesConfigureButton As Button
    Friend WithEvents SurnamesCheckBox As CheckBox
    Friend WithEvents StateAbbreviationsConfigureButton As Button
    Friend WithEvents StateAbbreviationsCheckBox As CheckBox
    Friend WithEvents StatesConfigureButton As Button
    Friend WithEvents StatesCheckBox As CheckBox
    Friend WithEvents SSNsConfigureButton As Button
    Friend WithEvents SSNsCheckBox As CheckBox
    Friend WithEvents PhoneNumberExtsConfigureButton As Button
    Friend WithEvents PhoneNumberExtsCheckBox As CheckBox
    Friend WithEvents PhoneNumbersConfigureButton As Button
    Friend WithEvents PhoneNumbersCheckBox As CheckBox
    Friend WithEvents NERConfigureButton As Button
    Friend WithEvents NERCheckBox As CheckBox
    Friend WithEvents IPAddressesConfigureButton As Button
    Friend WithEvents IPAddressesCheckBox As CheckBox
    Friend WithEvents HospitalsConfigureButton As Button
    Friend WithEvents HospitalsCheckBox As CheckBox
    Friend WithEvents HospitalAbbreviationsConfigureButton As Button
    Friend WithEvents HospitalAbbreviationsCheckBox As CheckBox
    Friend WithEvents FirstNamesConfigureButton As Button
    Friend WithEvents FirstNamesCheckBox As CheckBox
    Friend WithEvents EmailAddressesConfigureButton As Button
    Friend WithEvents EmailAddressesCheckBox As CheckBox
    Friend WithEvents DatesConfigureButton As Button
    Friend WithEvents DatesCheckBox As CheckBox
    Friend WithEvents CreditCardsConfigureButton As Button
    Friend WithEvents CreditCardsCheckBox As CheckBox
    Friend WithEvents CountiesConfigureButton As Button
    Friend WithEvents CountiesCheckBox As CheckBox
    Friend WithEvents CitiesConfigureButton As Button
    Friend WithEvents CitiesCheckBox As CheckBox
    Friend WithEvents AgesConfigureButton As Button
    Friend WithEvents AgesCheckBox As CheckBox
    Friend WithEvents PolicysToolStrip As ToolStrip
    Friend WithEvents ToolStripLabel1 As ToolStripLabel
    Friend WithEvents PolicysToolStripDropDownButton As ToolStripComboBox
    Friend WithEvents RefreshToolStripButton As ToolStripButton
    Friend WithEvents ToolStripSeparator6 As ToolStripSeparator
    Friend WithEvents NewToolStripButton As ToolStripButton
    Friend WithEvents SaveToolStripButton As ToolStripButton
    Friend WithEvents SaveAsToolStripButton As ToolStripButton
    Friend WithEvents DeleteToolStripButton As ToolStripButton
End Class
