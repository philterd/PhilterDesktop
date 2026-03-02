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
        PolicyPanel = New Panel()
        CustomIdentifiersConfigureButton = New Button()
        CustomIdentifiersCheckBox = New CheckBox()
        CustomDictionariesConfigureButton = New Button()
        CustomDictionariesCheckBox = New CheckBox()
        ZipCodesConfigureButton = New Button()
        ZipCodesCheckBox = New CheckBox()
        VINsConfigureButton = New Button()
        VINsCheckBox = New CheckBox()
        URLsConfigureButton = New Button()
        URLsCheckBox = New CheckBox()
        SurnamesConfigureButton = New Button()
        SurnamesCheckBox = New CheckBox()
        StateAbbreviationsConfigureButton = New Button()
        StateAbbreviationsCheckBox = New CheckBox()
        StatesConfigureButton = New Button()
        StatesCheckBox = New CheckBox()
        SSNsConfigureButton = New Button()
        SSNsCheckBox = New CheckBox()
        PhoneNumberExtsConfigureButton = New Button()
        PhoneNumberExtsCheckBox = New CheckBox()
        PhoneNumbersConfigureButton = New Button()
        PhoneNumbersCheckBox = New CheckBox()
        NERConfigureButton = New Button()
        NERCheckBox = New CheckBox()
        IPAddressesConfigureButton = New Button()
        IPAddressesCheckBox = New CheckBox()
        HospitalsConfigureButton = New Button()
        HospitalsCheckBox = New CheckBox()
        HospitalAbbreviationsConfigureButton = New Button()
        HospitalAbbreviationsCheckBox = New CheckBox()
        FirstNamesConfigureButton = New Button()
        FirstNamesCheckBox = New CheckBox()
        EmailAddressesConfigureButton = New Button()
        EmailAddressesCheckBox = New CheckBox()
        DatesConfigureButton = New Button()
        DatesCheckBox = New CheckBox()
        CreditCardsConfigureButton = New Button()
        CreditCardsCheckBox = New CheckBox()
        CountiesConfigureButton = New Button()
        CountiesCheckBox = New CheckBox()
        CitiesConfigureButton = New Button()
        CitiesCheckBox = New CheckBox()
        AgesConfigureButton = New Button()
        AgesCheckBox = New CheckBox()
        PolicysToolStrip = New ToolStrip()
        ToolStripLabel1 = New ToolStripLabel()
        PoliciesToolStripDropDownButton = New ToolStripComboBox()
        RefreshToolStripButton = New ToolStripButton()
        ToolStripSeparator6 = New ToolStripSeparator()
        NewToolStripButton = New ToolStripButton()
        SaveToolStripButton = New ToolStripButton()
        SaveAsToolStripButton = New ToolStripButton()
        DeleteToolStripButton = New ToolStripButton()
        PolicyPanel.SuspendLayout()
        PolicysToolStrip.SuspendLayout()
        SuspendLayout()
        ' 
        ' PolicyPanel
        ' 
        PolicyPanel.Controls.Add(CustomIdentifiersConfigureButton)
        PolicyPanel.Controls.Add(CustomIdentifiersCheckBox)
        PolicyPanel.Controls.Add(CustomDictionariesConfigureButton)
        PolicyPanel.Controls.Add(CustomDictionariesCheckBox)
        PolicyPanel.Controls.Add(ZipCodesConfigureButton)
        PolicyPanel.Controls.Add(ZipCodesCheckBox)
        PolicyPanel.Controls.Add(VINsConfigureButton)
        PolicyPanel.Controls.Add(VINsCheckBox)
        PolicyPanel.Controls.Add(URLsConfigureButton)
        PolicyPanel.Controls.Add(URLsCheckBox)
        PolicyPanel.Controls.Add(SurnamesConfigureButton)
        PolicyPanel.Controls.Add(SurnamesCheckBox)
        PolicyPanel.Controls.Add(StateAbbreviationsConfigureButton)
        PolicyPanel.Controls.Add(StateAbbreviationsCheckBox)
        PolicyPanel.Controls.Add(StatesConfigureButton)
        PolicyPanel.Controls.Add(StatesCheckBox)
        PolicyPanel.Controls.Add(SSNsConfigureButton)
        PolicyPanel.Controls.Add(SSNsCheckBox)
        PolicyPanel.Controls.Add(PhoneNumberExtsConfigureButton)
        PolicyPanel.Controls.Add(PhoneNumberExtsCheckBox)
        PolicyPanel.Controls.Add(PhoneNumbersConfigureButton)
        PolicyPanel.Controls.Add(PhoneNumbersCheckBox)
        PolicyPanel.Controls.Add(NERConfigureButton)
        PolicyPanel.Controls.Add(NERCheckBox)
        PolicyPanel.Controls.Add(IPAddressesConfigureButton)
        PolicyPanel.Controls.Add(IPAddressesCheckBox)
        PolicyPanel.Controls.Add(HospitalsConfigureButton)
        PolicyPanel.Controls.Add(HospitalsCheckBox)
        PolicyPanel.Controls.Add(HospitalAbbreviationsConfigureButton)
        PolicyPanel.Controls.Add(HospitalAbbreviationsCheckBox)
        PolicyPanel.Controls.Add(FirstNamesConfigureButton)
        PolicyPanel.Controls.Add(FirstNamesCheckBox)
        PolicyPanel.Controls.Add(EmailAddressesConfigureButton)
        PolicyPanel.Controls.Add(EmailAddressesCheckBox)
        PolicyPanel.Controls.Add(DatesConfigureButton)
        PolicyPanel.Controls.Add(DatesCheckBox)
        PolicyPanel.Controls.Add(CreditCardsConfigureButton)
        PolicyPanel.Controls.Add(CreditCardsCheckBox)
        PolicyPanel.Controls.Add(CountiesConfigureButton)
        PolicyPanel.Controls.Add(CountiesCheckBox)
        PolicyPanel.Controls.Add(CitiesConfigureButton)
        PolicyPanel.Controls.Add(CitiesCheckBox)
        PolicyPanel.Controls.Add(AgesConfigureButton)
        PolicyPanel.Controls.Add(AgesCheckBox)
        PolicyPanel.Dock = DockStyle.Fill
        PolicyPanel.Enabled = False
        PolicyPanel.Location = New Point(0, 31)
        PolicyPanel.Margin = New Padding(4, 3, 4, 3)
        PolicyPanel.Name = "PolicyPanel"
        PolicyPanel.Size = New Size(1074, 376)
        PolicyPanel.TabIndex = 10
        ' 
        ' CustomIdentifiersConfigureButton
        ' 
        CustomIdentifiersConfigureButton.Enabled = False
        CustomIdentifiersConfigureButton.Location = New Point(925, 58)
        CustomIdentifiersConfigureButton.Margin = New Padding(4, 3, 4, 3)
        CustomIdentifiersConfigureButton.Name = "CustomIdentifiersConfigureButton"
        CustomIdentifiersConfigureButton.Size = New Size(122, 27)
        CustomIdentifiersConfigureButton.TabIndex = 45
        CustomIdentifiersConfigureButton.Text = "Configure..."
        CustomIdentifiersConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' CustomIdentifiersCheckBox
        ' 
        CustomIdentifiersCheckBox.AutoSize = True
        CustomIdentifiersCheckBox.Location = New Point(765, 62)
        CustomIdentifiersCheckBox.Margin = New Padding(4, 3, 4, 3)
        CustomIdentifiersCheckBox.Name = "CustomIdentifiersCheckBox"
        CustomIdentifiersCheckBox.Size = New Size(123, 19)
        CustomIdentifiersCheckBox.TabIndex = 44
        CustomIdentifiersCheckBox.Text = "Custom Identifiers"
        CustomIdentifiersCheckBox.UseVisualStyleBackColor = True
        ' 
        ' CustomDictionariesConfigureButton
        ' 
        CustomDictionariesConfigureButton.Enabled = False
        CustomDictionariesConfigureButton.Location = New Point(925, 24)
        CustomDictionariesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        CustomDictionariesConfigureButton.Name = "CustomDictionariesConfigureButton"
        CustomDictionariesConfigureButton.Size = New Size(122, 27)
        CustomDictionariesConfigureButton.TabIndex = 43
        CustomDictionariesConfigureButton.Text = "Configure..."
        CustomDictionariesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' CustomDictionariesCheckBox
        ' 
        CustomDictionariesCheckBox.AutoSize = True
        CustomDictionariesCheckBox.Location = New Point(765, 29)
        CustomDictionariesCheckBox.Margin = New Padding(4, 3, 4, 3)
        CustomDictionariesCheckBox.Name = "CustomDictionariesCheckBox"
        CustomDictionariesCheckBox.Size = New Size(133, 19)
        CustomDictionariesCheckBox.TabIndex = 42
        CustomDictionariesCheckBox.Text = "Custom Dictionaries"
        CustomDictionariesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' ZipCodesConfigureButton
        ' 
        ZipCodesConfigureButton.Enabled = False
        ZipCodesConfigureButton.Location = New Point(554, 328)
        ZipCodesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        ZipCodesConfigureButton.Name = "ZipCodesConfigureButton"
        ZipCodesConfigureButton.Size = New Size(122, 27)
        ZipCodesConfigureButton.TabIndex = 41
        ZipCodesConfigureButton.Text = "Configure..."
        ZipCodesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' ZipCodesCheckBox
        ' 
        ZipCodesCheckBox.AutoSize = True
        ZipCodesCheckBox.Location = New Point(394, 332)
        ZipCodesCheckBox.Margin = New Padding(4, 3, 4, 3)
        ZipCodesCheckBox.Name = "ZipCodesCheckBox"
        ZipCodesCheckBox.Size = New Size(79, 19)
        ZipCodesCheckBox.TabIndex = 40
        ZipCodesCheckBox.Text = "Zip Codes"
        ZipCodesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' VINsConfigureButton
        ' 
        VINsConfigureButton.Enabled = False
        VINsConfigureButton.Location = New Point(554, 294)
        VINsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        VINsConfigureButton.Name = "VINsConfigureButton"
        VINsConfigureButton.Size = New Size(122, 27)
        VINsConfigureButton.TabIndex = 39
        VINsConfigureButton.Text = "Configure..."
        VINsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' VINsCheckBox
        ' 
        VINsCheckBox.AutoSize = True
        VINsCheckBox.Location = New Point(394, 299)
        VINsCheckBox.Margin = New Padding(4, 3, 4, 3)
        VINsCheckBox.Name = "VINsCheckBox"
        VINsCheckBox.Size = New Size(50, 19)
        VINsCheckBox.TabIndex = 38
        VINsCheckBox.Text = "VINs"
        VINsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' URLsConfigureButton
        ' 
        URLsConfigureButton.Enabled = False
        URLsConfigureButton.Location = New Point(554, 261)
        URLsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        URLsConfigureButton.Name = "URLsConfigureButton"
        URLsConfigureButton.Size = New Size(122, 27)
        URLsConfigureButton.TabIndex = 37
        URLsConfigureButton.Text = "Configure..."
        URLsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' URLsCheckBox
        ' 
        URLsCheckBox.AutoSize = True
        URLsCheckBox.Location = New Point(394, 265)
        URLsCheckBox.Margin = New Padding(4, 3, 4, 3)
        URLsCheckBox.Name = "URLsCheckBox"
        URLsCheckBox.Size = New Size(52, 19)
        URLsCheckBox.TabIndex = 36
        URLsCheckBox.Text = "URLs"
        URLsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' SurnamesConfigureButton
        ' 
        SurnamesConfigureButton.Enabled = False
        SurnamesConfigureButton.Location = New Point(554, 227)
        SurnamesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        SurnamesConfigureButton.Name = "SurnamesConfigureButton"
        SurnamesConfigureButton.Size = New Size(122, 27)
        SurnamesConfigureButton.TabIndex = 35
        SurnamesConfigureButton.Text = "Configure..."
        SurnamesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' SurnamesCheckBox
        ' 
        SurnamesCheckBox.AutoSize = True
        SurnamesCheckBox.Location = New Point(394, 232)
        SurnamesCheckBox.Margin = New Padding(4, 3, 4, 3)
        SurnamesCheckBox.Name = "SurnamesCheckBox"
        SurnamesCheckBox.Size = New Size(78, 19)
        SurnamesCheckBox.TabIndex = 34
        SurnamesCheckBox.Text = "Surnames"
        SurnamesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' StateAbbreviationsConfigureButton
        ' 
        StateAbbreviationsConfigureButton.Enabled = False
        StateAbbreviationsConfigureButton.Location = New Point(554, 194)
        StateAbbreviationsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        StateAbbreviationsConfigureButton.Name = "StateAbbreviationsConfigureButton"
        StateAbbreviationsConfigureButton.Size = New Size(122, 27)
        StateAbbreviationsConfigureButton.TabIndex = 33
        StateAbbreviationsConfigureButton.Text = "Configure..."
        StateAbbreviationsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' StateAbbreviationsCheckBox
        ' 
        StateAbbreviationsCheckBox.AutoSize = True
        StateAbbreviationsCheckBox.Location = New Point(394, 198)
        StateAbbreviationsCheckBox.Margin = New Padding(4, 3, 4, 3)
        StateAbbreviationsCheckBox.Name = "StateAbbreviationsCheckBox"
        StateAbbreviationsCheckBox.Size = New Size(128, 19)
        StateAbbreviationsCheckBox.TabIndex = 32
        StateAbbreviationsCheckBox.Text = "State Abbreviations"
        StateAbbreviationsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' StatesConfigureButton
        ' 
        StatesConfigureButton.Enabled = False
        StatesConfigureButton.Location = New Point(554, 160)
        StatesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        StatesConfigureButton.Name = "StatesConfigureButton"
        StatesConfigureButton.Size = New Size(122, 27)
        StatesConfigureButton.TabIndex = 31
        StatesConfigureButton.Text = "Configure..."
        StatesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' StatesCheckBox
        ' 
        StatesCheckBox.AutoSize = True
        StatesCheckBox.Location = New Point(394, 165)
        StatesCheckBox.Margin = New Padding(4, 3, 4, 3)
        StatesCheckBox.Name = "StatesCheckBox"
        StatesCheckBox.Size = New Size(57, 19)
        StatesCheckBox.TabIndex = 30
        StatesCheckBox.Text = "States"
        StatesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' SSNsConfigureButton
        ' 
        SSNsConfigureButton.Enabled = False
        SSNsConfigureButton.Location = New Point(554, 127)
        SSNsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        SSNsConfigureButton.Name = "SSNsConfigureButton"
        SSNsConfigureButton.Size = New Size(122, 27)
        SSNsConfigureButton.TabIndex = 29
        SSNsConfigureButton.Text = "Configure..."
        SSNsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' SSNsCheckBox
        ' 
        SSNsCheckBox.AutoSize = True
        SSNsCheckBox.Location = New Point(394, 132)
        SSNsCheckBox.Margin = New Padding(4, 3, 4, 3)
        SSNsCheckBox.Name = "SSNsCheckBox"
        SSNsCheckBox.Size = New Size(52, 19)
        SSNsCheckBox.TabIndex = 28
        SSNsCheckBox.Text = "SSNs"
        SSNsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' PhoneNumberExtsConfigureButton
        ' 
        PhoneNumberExtsConfigureButton.Enabled = False
        PhoneNumberExtsConfigureButton.Location = New Point(554, 93)
        PhoneNumberExtsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        PhoneNumberExtsConfigureButton.Name = "PhoneNumberExtsConfigureButton"
        PhoneNumberExtsConfigureButton.Size = New Size(122, 27)
        PhoneNumberExtsConfigureButton.TabIndex = 27
        PhoneNumberExtsConfigureButton.Text = "Configure..."
        PhoneNumberExtsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' PhoneNumberExtsCheckBox
        ' 
        PhoneNumberExtsCheckBox.AutoSize = True
        PhoneNumberExtsCheckBox.Location = New Point(394, 98)
        PhoneNumberExtsCheckBox.Margin = New Padding(4, 3, 4, 3)
        PhoneNumberExtsCheckBox.Name = "PhoneNumberExtsCheckBox"
        PhoneNumberExtsCheckBox.Size = New Size(133, 19)
        PhoneNumberExtsCheckBox.TabIndex = 26
        PhoneNumberExtsCheckBox.Text = "Phone Number Exts."
        PhoneNumberExtsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' PhoneNumbersConfigureButton
        ' 
        PhoneNumbersConfigureButton.Enabled = False
        PhoneNumbersConfigureButton.Location = New Point(554, 60)
        PhoneNumbersConfigureButton.Margin = New Padding(4, 3, 4, 3)
        PhoneNumbersConfigureButton.Name = "PhoneNumbersConfigureButton"
        PhoneNumbersConfigureButton.Size = New Size(122, 27)
        PhoneNumbersConfigureButton.TabIndex = 25
        PhoneNumbersConfigureButton.Text = "Configure..."
        PhoneNumbersConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' PhoneNumbersCheckBox
        ' 
        PhoneNumbersCheckBox.AutoSize = True
        PhoneNumbersCheckBox.Location = New Point(394, 65)
        PhoneNumbersCheckBox.Margin = New Padding(4, 3, 4, 3)
        PhoneNumbersCheckBox.Name = "PhoneNumbersCheckBox"
        PhoneNumbersCheckBox.Size = New Size(112, 19)
        PhoneNumbersCheckBox.TabIndex = 24
        PhoneNumbersCheckBox.Text = "Phone Numbers"
        PhoneNumbersCheckBox.UseVisualStyleBackColor = True
        ' 
        ' NERConfigureButton
        ' 
        NERConfigureButton.Enabled = False
        NERConfigureButton.Location = New Point(554, 27)
        NERConfigureButton.Margin = New Padding(4, 3, 4, 3)
        NERConfigureButton.Name = "NERConfigureButton"
        NERConfigureButton.Size = New Size(122, 27)
        NERConfigureButton.TabIndex = 23
        NERConfigureButton.Text = "Configure..."
        NERConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' NERCheckBox
        ' 
        NERCheckBox.AutoSize = True
        NERCheckBox.Location = New Point(394, 31)
        NERCheckBox.Margin = New Padding(4, 3, 4, 3)
        NERCheckBox.Name = "NERCheckBox"
        NERCheckBox.Size = New Size(141, 19)
        NERCheckBox.TabIndex = 22
        NERCheckBox.Text = "NER (English Persons)"
        NERCheckBox.UseVisualStyleBackColor = True
        ' 
        ' IPAddressesConfigureButton
        ' 
        IPAddressesConfigureButton.Enabled = False
        IPAddressesConfigureButton.Location = New Point(182, 323)
        IPAddressesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        IPAddressesConfigureButton.Name = "IPAddressesConfigureButton"
        IPAddressesConfigureButton.Size = New Size(122, 27)
        IPAddressesConfigureButton.TabIndex = 21
        IPAddressesConfigureButton.Text = "Configure..."
        IPAddressesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' IPAddressesCheckBox
        ' 
        IPAddressesCheckBox.AutoSize = True
        IPAddressesCheckBox.Location = New Point(22, 328)
        IPAddressesCheckBox.Margin = New Padding(4, 3, 4, 3)
        IPAddressesCheckBox.Name = "IPAddressesCheckBox"
        IPAddressesCheckBox.Size = New Size(92, 19)
        IPAddressesCheckBox.TabIndex = 20
        IPAddressesCheckBox.Text = "IP Addresses"
        IPAddressesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' HospitalsConfigureButton
        ' 
        HospitalsConfigureButton.Enabled = False
        HospitalsConfigureButton.Location = New Point(182, 290)
        HospitalsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        HospitalsConfigureButton.Name = "HospitalsConfigureButton"
        HospitalsConfigureButton.Size = New Size(122, 27)
        HospitalsConfigureButton.TabIndex = 17
        HospitalsConfigureButton.Text = "Configure..."
        HospitalsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' HospitalsCheckBox
        ' 
        HospitalsCheckBox.AutoSize = True
        HospitalsCheckBox.Location = New Point(22, 294)
        HospitalsCheckBox.Margin = New Padding(4, 3, 4, 3)
        HospitalsCheckBox.Name = "HospitalsCheckBox"
        HospitalsCheckBox.Size = New Size(75, 19)
        HospitalsCheckBox.TabIndex = 16
        HospitalsCheckBox.Text = "Hospitals"
        HospitalsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' HospitalAbbreviationsConfigureButton
        ' 
        HospitalAbbreviationsConfigureButton.Enabled = False
        HospitalAbbreviationsConfigureButton.Location = New Point(182, 256)
        HospitalAbbreviationsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        HospitalAbbreviationsConfigureButton.Name = "HospitalAbbreviationsConfigureButton"
        HospitalAbbreviationsConfigureButton.Size = New Size(122, 27)
        HospitalAbbreviationsConfigureButton.TabIndex = 15
        HospitalAbbreviationsConfigureButton.Text = "Configure..."
        HospitalAbbreviationsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' HospitalAbbreviationsCheckBox
        ' 
        HospitalAbbreviationsCheckBox.AutoSize = True
        HospitalAbbreviationsCheckBox.Location = New Point(22, 261)
        HospitalAbbreviationsCheckBox.Margin = New Padding(4, 3, 4, 3)
        HospitalAbbreviationsCheckBox.Name = "HospitalAbbreviationsCheckBox"
        HospitalAbbreviationsCheckBox.Size = New Size(146, 19)
        HospitalAbbreviationsCheckBox.TabIndex = 14
        HospitalAbbreviationsCheckBox.Text = "Hospital Abbreviations"
        HospitalAbbreviationsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' FirstNamesConfigureButton
        ' 
        FirstNamesConfigureButton.Enabled = False
        FirstNamesConfigureButton.Location = New Point(182, 223)
        FirstNamesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        FirstNamesConfigureButton.Name = "FirstNamesConfigureButton"
        FirstNamesConfigureButton.Size = New Size(122, 27)
        FirstNamesConfigureButton.TabIndex = 13
        FirstNamesConfigureButton.Text = "Configure..."
        FirstNamesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' FirstNamesCheckBox
        ' 
        FirstNamesCheckBox.AutoSize = True
        FirstNamesCheckBox.Location = New Point(22, 227)
        FirstNamesCheckBox.Margin = New Padding(4, 3, 4, 3)
        FirstNamesCheckBox.Name = "FirstNamesCheckBox"
        FirstNamesCheckBox.Size = New Size(88, 19)
        FirstNamesCheckBox.TabIndex = 12
        FirstNamesCheckBox.Text = "First Names"
        FirstNamesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' EmailAddressesConfigureButton
        ' 
        EmailAddressesConfigureButton.Enabled = False
        EmailAddressesConfigureButton.Location = New Point(182, 189)
        EmailAddressesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        EmailAddressesConfigureButton.Name = "EmailAddressesConfigureButton"
        EmailAddressesConfigureButton.Size = New Size(122, 27)
        EmailAddressesConfigureButton.TabIndex = 11
        EmailAddressesConfigureButton.Text = "Configure..."
        EmailAddressesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' EmailAddressesCheckBox
        ' 
        EmailAddressesCheckBox.AutoSize = True
        EmailAddressesCheckBox.Location = New Point(22, 194)
        EmailAddressesCheckBox.Margin = New Padding(4, 3, 4, 3)
        EmailAddressesCheckBox.Name = "EmailAddressesCheckBox"
        EmailAddressesCheckBox.Size = New Size(100, 19)
        EmailAddressesCheckBox.TabIndex = 10
        EmailAddressesCheckBox.Text = "Email Address"
        EmailAddressesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' DatesConfigureButton
        ' 
        DatesConfigureButton.Enabled = False
        DatesConfigureButton.Location = New Point(182, 156)
        DatesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        DatesConfigureButton.Name = "DatesConfigureButton"
        DatesConfigureButton.Size = New Size(122, 27)
        DatesConfigureButton.TabIndex = 9
        DatesConfigureButton.Text = "Configure..."
        DatesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' DatesCheckBox
        ' 
        DatesCheckBox.AutoSize = True
        DatesCheckBox.Location = New Point(22, 160)
        DatesCheckBox.Margin = New Padding(4, 3, 4, 3)
        DatesCheckBox.Name = "DatesCheckBox"
        DatesCheckBox.Size = New Size(55, 19)
        DatesCheckBox.TabIndex = 8
        DatesCheckBox.Text = "Dates"
        DatesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' CreditCardsConfigureButton
        ' 
        CreditCardsConfigureButton.Enabled = False
        CreditCardsConfigureButton.Location = New Point(182, 122)
        CreditCardsConfigureButton.Margin = New Padding(4, 3, 4, 3)
        CreditCardsConfigureButton.Name = "CreditCardsConfigureButton"
        CreditCardsConfigureButton.Size = New Size(122, 27)
        CreditCardsConfigureButton.TabIndex = 7
        CreditCardsConfigureButton.Text = "Configure..."
        CreditCardsConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' CreditCardsCheckBox
        ' 
        CreditCardsCheckBox.AutoSize = True
        CreditCardsCheckBox.Location = New Point(22, 127)
        CreditCardsCheckBox.Margin = New Padding(4, 3, 4, 3)
        CreditCardsCheckBox.Name = "CreditCardsCheckBox"
        CreditCardsCheckBox.Size = New Size(91, 19)
        CreditCardsCheckBox.TabIndex = 6
        CreditCardsCheckBox.Text = "Credit Cards"
        CreditCardsCheckBox.UseVisualStyleBackColor = True
        ' 
        ' CountiesConfigureButton
        ' 
        CountiesConfigureButton.Enabled = False
        CountiesConfigureButton.Location = New Point(182, 89)
        CountiesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        CountiesConfigureButton.Name = "CountiesConfigureButton"
        CountiesConfigureButton.Size = New Size(122, 27)
        CountiesConfigureButton.TabIndex = 5
        CountiesConfigureButton.Text = "Configure..."
        CountiesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' CountiesCheckBox
        ' 
        CountiesCheckBox.AutoSize = True
        CountiesCheckBox.Location = New Point(22, 93)
        CountiesCheckBox.Margin = New Padding(4, 3, 4, 3)
        CountiesCheckBox.Name = "CountiesCheckBox"
        CountiesCheckBox.Size = New Size(73, 19)
        CountiesCheckBox.TabIndex = 4
        CountiesCheckBox.Text = "Counties"
        CountiesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' CitiesConfigureButton
        ' 
        CitiesConfigureButton.Enabled = False
        CitiesConfigureButton.Location = New Point(182, 55)
        CitiesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        CitiesConfigureButton.Name = "CitiesConfigureButton"
        CitiesConfigureButton.Size = New Size(122, 27)
        CitiesConfigureButton.TabIndex = 3
        CitiesConfigureButton.Text = "Configure..."
        CitiesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' CitiesCheckBox
        ' 
        CitiesCheckBox.AutoSize = True
        CitiesCheckBox.Location = New Point(22, 60)
        CitiesCheckBox.Margin = New Padding(4, 3, 4, 3)
        CitiesCheckBox.Name = "CitiesCheckBox"
        CitiesCheckBox.Size = New Size(55, 19)
        CitiesCheckBox.TabIndex = 2
        CitiesCheckBox.Text = "Cities"
        CitiesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' AgesConfigureButton
        ' 
        AgesConfigureButton.Enabled = False
        AgesConfigureButton.Location = New Point(182, 22)
        AgesConfigureButton.Margin = New Padding(4, 3, 4, 3)
        AgesConfigureButton.Name = "AgesConfigureButton"
        AgesConfigureButton.Size = New Size(122, 27)
        AgesConfigureButton.TabIndex = 1
        AgesConfigureButton.Text = "Configure..."
        AgesConfigureButton.UseVisualStyleBackColor = True
        ' 
        ' AgesCheckBox
        ' 
        AgesCheckBox.AutoSize = True
        AgesCheckBox.Location = New Point(22, 27)
        AgesCheckBox.Margin = New Padding(4, 3, 4, 3)
        AgesCheckBox.Name = "AgesCheckBox"
        AgesCheckBox.Size = New Size(52, 19)
        AgesCheckBox.TabIndex = 0
        AgesCheckBox.Text = "Ages"
        AgesCheckBox.UseVisualStyleBackColor = True
        ' 
        ' PolicysToolStrip
        ' 
        PolicysToolStrip.ImageScalingSize = New Size(24, 24)
        PolicysToolStrip.Items.AddRange(New ToolStripItem() {ToolStripLabel1, PoliciesToolStripDropDownButton, RefreshToolStripButton, ToolStripSeparator6, NewToolStripButton, SaveToolStripButton, SaveAsToolStripButton, DeleteToolStripButton})
        PolicysToolStrip.Location = New Point(0, 0)
        PolicysToolStrip.Name = "PolicysToolStrip"
        PolicysToolStrip.Padding = New Padding(0, 0, 2, 0)
        PolicysToolStrip.Size = New Size(1074, 31)
        PolicysToolStrip.TabIndex = 9
        PolicysToolStrip.Text = "ToolStrip3"
        ' 
        ' ToolStripLabel1
        ' 
        ToolStripLabel1.Name = "ToolStripLabel1"
        ToolStripLabel1.Size = New Size(85, 28)
        ToolStripLabel1.Text = "Select a Policy:"
        ' 
        ' PoliciesToolStripDropDownButton
        ' 
        PoliciesToolStripDropDownButton.DropDownStyle = ComboBoxStyle.DropDownList
        PoliciesToolStripDropDownButton.Name = "PoliciesToolStripDropDownButton"
        PoliciesToolStripDropDownButton.Size = New Size(320, 31)
        ' 
        ' RefreshToolStripButton
        ' 
        RefreshToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        RefreshToolStripButton.Image = CType(resources.GetObject("RefreshToolStripButton.Image"), Image)
        RefreshToolStripButton.ImageTransparentColor = Color.Magenta
        RefreshToolStripButton.Name = "RefreshToolStripButton"
        RefreshToolStripButton.Size = New Size(28, 28)
        RefreshToolStripButton.Text = "Refresh"
        ' 
        ' ToolStripSeparator6
        ' 
        ToolStripSeparator6.Name = "ToolStripSeparator6"
        ToolStripSeparator6.Size = New Size(6, 31)
        ' 
        ' NewToolStripButton
        ' 
        NewToolStripButton.Image = CType(resources.GetObject("NewToolStripButton.Image"), Image)
        NewToolStripButton.ImageTransparentColor = Color.Magenta
        NewToolStripButton.Name = "NewToolStripButton"
        NewToolStripButton.Size = New Size(68, 28)
        NewToolStripButton.Text = "New..."
        ' 
        ' SaveToolStripButton
        ' 
        SaveToolStripButton.Enabled = False
        SaveToolStripButton.Image = CType(resources.GetObject("SaveToolStripButton.Image"), Image)
        SaveToolStripButton.ImageTransparentColor = Color.Magenta
        SaveToolStripButton.Name = "SaveToolStripButton"
        SaveToolStripButton.Size = New Size(59, 28)
        SaveToolStripButton.Text = "Save"
        ' 
        ' SaveAsToolStripButton
        ' 
        SaveAsToolStripButton.Enabled = False
        SaveAsToolStripButton.Image = CType(resources.GetObject("SaveAsToolStripButton.Image"), Image)
        SaveAsToolStripButton.ImageTransparentColor = Color.Magenta
        SaveAsToolStripButton.Name = "SaveAsToolStripButton"
        SaveAsToolStripButton.Size = New Size(84, 28)
        SaveAsToolStripButton.Text = "Save As..."
        ' 
        ' DeleteToolStripButton
        ' 
        DeleteToolStripButton.Enabled = False
        DeleteToolStripButton.Image = CType(resources.GetObject("DeleteToolStripButton.Image"), Image)
        DeleteToolStripButton.ImageTransparentColor = Color.Magenta
        DeleteToolStripButton.Name = "DeleteToolStripButton"
        DeleteToolStripButton.Size = New Size(77, 28)
        DeleteToolStripButton.Text = "Delete..."
        ' 
        ' PolicyEditorForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1074, 407)
        Controls.Add(PolicyPanel)
        Controls.Add(PolicysToolStrip)
        FormBorderStyle = FormBorderStyle.FixedDialog
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4, 3, 4, 3)
        MaximizeBox = False
        MinimumSize = New Size(1089, 442)
        Name = "PolicyEditorForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Philter Policy Editor"
        PolicyPanel.ResumeLayout(False)
        PolicyPanel.PerformLayout()
        PolicysToolStrip.ResumeLayout(False)
        PolicysToolStrip.PerformLayout()
        ResumeLayout(False)
        PerformLayout()

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
    Friend WithEvents PoliciesToolStripDropDownButton As ToolStripComboBox
    Friend WithEvents RefreshToolStripButton As ToolStripButton
    Friend WithEvents ToolStripSeparator6 As ToolStripSeparator
    Friend WithEvents NewToolStripButton As ToolStripButton
    Friend WithEvents SaveToolStripButton As ToolStripButton
    Friend WithEvents SaveAsToolStripButton As ToolStripButton
    Friend WithEvents DeleteToolStripButton As ToolStripButton
End Class
