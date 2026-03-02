Imports System.Windows.Forms
Imports Newtonsoft.Json
Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters
Imports PhilterData
Imports PhilterDesktop

Public Class PolicyEditorForm

    Dim _repo As PolicyRepository

    Public Sub New(repo As PolicyRepository)
        InitializeComponent()
        _repo = repo
    End Sub

    Private Sub Policys_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' TODO: Load the policy names
    End Sub

    Private Sub NewToolStripButton_Click(sender As Object, e As EventArgs) Handles NewToolStripButton.Click

        Dim PolicyEntity as new PolicyEntity
        PolicyEntity.Name = "new policy3"
        _repo.Insert(PolicyEntity)

        PoliciesToolStripDropDownButton.Items.Clear()
        For each policy as PolicyEntity in _repo.GetAll()
            PoliciesToolStripDropDownButton.Items.Add(policy.Name)
        Next

    End Sub

    Private Sub ResetForm()

        AgesCheckBox.Checked = False
        CitiesCheckBox.Checked = False
        CountiesCheckBox.Checked = False
        CreditCardsCheckBox.Checked = False
        DatesCheckBox.Checked = False
        EmailAddressesCheckBox.Checked = False
        FirstNamesCheckBox.Checked = False
        HospitalAbbreviationsCheckBox.Checked = False
        HospitalsCheckBox.Checked = False
        IPAddressesCheckBox.Checked = False
        NERCheckBox.Checked = False
        PhoneNumbersCheckBox.Checked = False
        PhoneNumberExtsCheckBox.Checked = False
        SSNsCheckBox.Checked = False
        StatesCheckBox.Checked = False
        StateAbbreviationsCheckBox.Checked = False
        SurnamesCheckBox.Checked = False
        URLsCheckBox.Checked = False
        VINsCheckBox.Checked = False
        ZipCodesCheckBox.Checked = False
        CustomDictionariesCheckBox.Checked = False
        CustomIdentifiersCheckBox.Checked = False

    End Sub

    Private Sub SaveToolStripButton_Click(sender As Object, e As EventArgs) Handles SaveToolStripButton.Click

        ' Save the policy to the database.
        Dim PolicyEntity As New PolicyEntity
        PolicyEntity.Name = "saved"
        PolicyEntity.Json = JsonConvert.SerializeObject(GetPolicy())
        _repo.Insert(PolicyEntity)

    End Sub

    Private Sub SaveAsToolStripButton_Click(sender As Object, e As EventArgs) Handles SaveAsToolStripButton.Click

        ' TODO: Save the policy under a new file name.

    End Sub

    Private Sub RefreshToolStripButton_Click(sender As Object, e As EventArgs) Handles RefreshToolStripButton.Click

        ' Refresh the list of policies.
        PoliciesToolStripDropDownButton.Items.Clear()
        For each policy as PolicyEntity in _repo.GetAll()
            PoliciesToolStripDropDownButton.Items.Add(policy.Name)
        Next

    End Sub

    Private Sub DeleteToolStripButton_Click(sender As Object, e As EventArgs) Handles DeleteToolStripButton.Click

        ' TODO: Get the policy name.
        If MessageBox.Show("Are you sure you want to delete the policy with name " & "test" & "?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then

            ' Delete the policy from the database.
            _repo.Delete("new policy")

            For each policy as PolicyEntity in _repo.GetAll()
                PoliciesToolStripDropDownButton.Items.Add(policy.Name)
            Next

            ResetForm()

        End If

    End Sub

    Private Sub PolicysToolStripDropDownButton_SelectedIndexChanged(sender As Object, e As EventArgs) Handles PoliciesToolStripDropDownButton.SelectedIndexChanged

        Dim Name As String = PoliciesToolStripDropDownButton.SelectedItem.ToString

        Dim PolicyEntity As PolicyEntity = _repo.FindOne(Function(x) x.Name.Equals(Name))
        Dim json As String = PolicyEntity.Json
        System.Diagnostics.Debug.WriteLine(json)

        Dim fp As Policy = JsonConvert.DeserializeObject(Of Policy)(json)

        ResetForm()

        PolicyPanel.Tag = fp

        If Not fp.Identifiers is Nothing then

            If not fp.Identifiers.Age is Nothing Then
                AgesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.City Is Nothing Then
                CitiesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.County Is Nothing Then
                CountiesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.CreditCard Is Nothing Then
                CreditCardsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.CustomDictionaries Is Nothing Then
                CustomDictionariesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Date Is Nothing Then
                DatesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.EmailAddress Is Nothing Then
                EmailAddressesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.FirstName Is Nothing Then
                FirstNamesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Hospital Is Nothing Then
                HospitalsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.HospitalAbbreviation Is Nothing Then
                HospitalAbbreviationsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.CustomIdentifiers Is Nothing Then
                CustomIdentifiersCheckBox.Checked = True
            End If

            If Not fp.Identifiers.IpAddress Is Nothing Then
                IPAddressesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Ner Is Nothing Then
                NERCheckBox.Checked = True
            End If

            If Not fp.Identifiers.PhoneNumber Is Nothing Then
                PhoneNumbersCheckBox.Checked = True
            End If

            If Not fp.Identifiers.PhoneNumberExtension Is Nothing Then
                PhoneNumberExtsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Ssn Is Nothing Then
                SSNsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.State Is Nothing Then
                StatesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.StateAbbreviation Is Nothing Then
                StateAbbreviationsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Surname Is Nothing Then
                SurnamesCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Url Is Nothing Then
                URLsCheckBox.Checked = True
            End If

            If Not fp.Identifiers.Vin Is Nothing Then
                VINsCheckBox.Checked = True
            End If

        Else

            fp.Identifiers = new Identifiers()
            PolicyPanel.Tag = fp

        End If

        PolicyPanel.Enabled = True

        SaveToolStripButton.Enabled = True
        SaveAsToolStripButton.Enabled = True
        DeleteToolStripButton.Enabled = True

    End Sub

    Private Function LoadPolicyNames() As List(Of String)

        Dim PolicyNames As New List(Of String)

        ' TOOD: Get the policy names from the database.

        Return PolicyNames

    End Function

    Private Function GetPolicy() As Policy

        Return DirectCast(PolicyPanel.Tag, Policy)

    End Function

    Private Sub AgesConfigureButton_Click(sender As Object, e As EventArgs) Handles AgesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Age Is Nothing Then
            Policy.Identifiers.Age = New Filters.Age
        End If

        Dim fs As New AgeFilterStrategiesForm(Policy, Age.GetName(), Policy.Identifiers.Age.AgeFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Age.AgeFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Age = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub AgesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles AgesCheckBox.CheckedChanged

        AgesConfigureButton.Enabled = AgesCheckBox.Checked

        If AgesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Age Is Nothing Then
                GetPolicy().Identifiers().Age = New Filters.Age
            End If

            GetPolicy().Identifiers().Age.Enabled = AgesCheckBox.Checked

        Else

            GetPolicy().Identifiers().Age = Nothing

        End If

    End Sub

    Private Sub CitiesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CitiesCheckBox.CheckedChanged

        CitiesConfigureButton.Enabled = CitiesCheckBox.Checked

        If CitiesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().City Is Nothing Then
                GetPolicy().Identifiers().City = New Filters.City
            End If

            GetPolicy().Identifiers().City.Enabled = CitiesCheckBox.Checked

        Else

            GetPolicy().Identifiers().City = Nothing

        End If

    End Sub

    Private Sub CountiesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CountiesCheckBox.CheckedChanged

        CountiesConfigureButton.Enabled = CountiesCheckBox.Checked

        If CountiesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().County Is Nothing Then
                GetPolicy().Identifiers().County = New Filters.County
            End If

            GetPolicy().Identifiers().County.Enabled = CountiesCheckBox.Checked

        Else

            GetPolicy().Identifiers().County = Nothing

        End If

    End Sub

    Private Sub CreditCardsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CreditCardsCheckBox.CheckedChanged

        CreditCardsConfigureButton.Enabled = CreditCardsCheckBox.Checked

        If CreditCardsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().CreditCard Is Nothing Then
                GetPolicy().Identifiers().CreditCard = New Filters.CreditCard
            End If

            GetPolicy().Identifiers().CreditCard.Enabled = CreditCardsCheckBox.Checked

        Else

            GetPolicy().Identifiers().CreditCard = Nothing

        End If

    End Sub

    Private Sub DatesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DatesCheckBox.CheckedChanged

        DatesConfigureButton.Enabled = DatesCheckBox.Checked

        If DatesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Date Is Nothing Then
                GetPolicy().Identifiers().Date = New Filters.Date
            End If

            GetPolicy().Identifiers().Date.Enabled = DatesCheckBox.Checked

        Else

            GetPolicy().Identifiers().Date = Nothing

        End If

    End Sub

    Private Sub EmailAddressesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles EmailAddressesCheckBox.CheckedChanged

        EmailAddressesConfigureButton.Enabled = EmailAddressesCheckBox.Checked

        If EmailAddressesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().EmailAddress Is Nothing Then
                GetPolicy().Identifiers().EmailAddress = New Filters.EmailAddress
            End If

            GetPolicy().Identifiers().EmailAddress.Enabled = EmailAddressesCheckBox.Checked

        Else

            GetPolicy().Identifiers().EmailAddress = Nothing

        End If

    End Sub

    Private Sub FirstNamesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles FirstNamesCheckBox.CheckedChanged

        FirstNamesConfigureButton.Enabled = FirstNamesCheckBox.Checked

        If FirstNamesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().FirstName Is Nothing Then
                GetPolicy().Identifiers().FirstName = New Filters.FirstName
            End If

            GetPolicy().Identifiers().FirstName.Enabled = FirstNamesCheckBox.Checked

        Else

            GetPolicy().Identifiers().FirstName = Nothing

        End If

    End Sub

    Private Sub HospitalAbbreviationsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles HospitalAbbreviationsCheckBox.CheckedChanged

        HospitalAbbreviationsConfigureButton.Enabled = HospitalAbbreviationsCheckBox.Checked

        If HospitalAbbreviationsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().HospitalAbbreviation Is Nothing Then
                GetPolicy().Identifiers().HospitalAbbreviation = New Filters.HospitalAbbreviation
            End If

            GetPolicy().Identifiers().HospitalAbbreviation.Enabled = HospitalAbbreviationsCheckBox.Checked

        Else

            GetPolicy().Identifiers().HospitalAbbreviation = Nothing

        End If

    End Sub

    Private Sub HospitalsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles HospitalsCheckBox.CheckedChanged

        HospitalsConfigureButton.Enabled = HospitalsCheckBox.Checked

        If HospitalsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Hospital Is Nothing Then
                GetPolicy().Identifiers().Hospital = New Filters.Hospital
            End If

            GetPolicy().Identifiers().Hospital.Enabled = HospitalsCheckBox.Checked

        Else

            GetPolicy().Identifiers().Hospital = Nothing

        End If

    End Sub

    Private Sub CheckBox19_CheckedChanged(sender As Object, e As EventArgs) Handles IPAddressesCheckBox.CheckedChanged

        IPAddressesConfigureButton.Enabled = IPAddressesCheckBox.Checked

        If IPAddressesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().IpAddress Is Nothing Then
                GetPolicy().Identifiers().IpAddress = New Filters.IpAddress
            End If

            GetPolicy().Identifiers().IpAddress.Enabled = IPAddressesCheckBox.Checked

        Else

            GetPolicy().Identifiers().IpAddress = Nothing

        End If

    End Sub

    Private Sub NERCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles NERCheckBox.CheckedChanged

        NERConfigureButton.Enabled = NERCheckBox.Checked

        If NERCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Ner Is Nothing Then
                GetPolicy().Identifiers().Ner = New Filters.Ner
            End If

            GetPolicy().Identifiers().Ner.Enabled = NERCheckBox.Checked

        Else

            GetPolicy().Identifiers().Ner = Nothing

        End If

    End Sub

    Private Sub PhoneNumbersCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles PhoneNumbersCheckBox.CheckedChanged

        PhoneNumbersConfigureButton.Enabled = PhoneNumbersCheckBox.Checked

        If PhoneNumbersCheckBox.Checked = True Then

            If GetPolicy().Identifiers().PhoneNumber Is Nothing Then
                GetPolicy().Identifiers().PhoneNumber = New Filters.PhoneNumber
            End If

            GetPolicy().Identifiers().PhoneNumber.Enabled = PhoneNumbersCheckBox.Checked

        Else

            GetPolicy().Identifiers().PhoneNumber = Nothing

        End If

    End Sub

    Private Sub PhoneNumberExtsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles PhoneNumberExtsCheckBox.CheckedChanged

        PhoneNumberExtsConfigureButton.Enabled = PhoneNumberExtsCheckBox.Checked

        If PhoneNumberExtsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().PhoneNumberExtension Is Nothing Then
                GetPolicy().Identifiers().PhoneNumberExtension = New Filters.PhoneNumberExtension
            End If

            GetPolicy().Identifiers().PhoneNumberExtension.Enabled = PhoneNumberExtsCheckBox.Checked

        Else

            GetPolicy().Identifiers().PhoneNumberExtension = Nothing

        End If

    End Sub

    Private Sub SSNsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles SSNsCheckBox.CheckedChanged

        SSNsConfigureButton.Enabled = SSNsCheckBox.Checked

        If SSNsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Ssn Is Nothing Then
                GetPolicy().Identifiers().Ssn = New Filters.Ssn
            End If

            GetPolicy().Identifiers().Ssn.Enabled = SSNsCheckBox.Checked

        Else

            GetPolicy().Identifiers().Ssn = Nothing

        End If

    End Sub

    Private Sub StatesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles StatesCheckBox.CheckedChanged

        StatesConfigureButton.Enabled = StatesCheckBox.Checked

        If StatesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().State Is Nothing Then
                GetPolicy().Identifiers().State = New Filters.State
            End If

            GetPolicy().Identifiers().State.Enabled = StatesCheckBox.Checked

        Else

            GetPolicy().Identifiers().State = Nothing

        End If

    End Sub

    Private Sub StateAbbreviationsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles StateAbbreviationsCheckBox.CheckedChanged

        StateAbbreviationsConfigureButton.Enabled = StateAbbreviationsCheckBox.Checked

        If StateAbbreviationsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().StateAbbreviation Is Nothing Then
                GetPolicy().Identifiers().StateAbbreviation = New Filters.StateAbbreviation
            End If

            GetPolicy().Identifiers().StateAbbreviation.Enabled = StateAbbreviationsCheckBox.Checked

        Else

            GetPolicy().Identifiers().StateAbbreviation = Nothing

        End If

    End Sub

    Private Sub SurnamesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles SurnamesCheckBox.CheckedChanged

        SurnamesConfigureButton.Enabled = SurnamesCheckBox.Checked

        If SurnamesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Surname Is Nothing Then
                GetPolicy().Identifiers().Surname = New Filters.Surname
            End If

            GetPolicy().Identifiers().Surname.Enabled = SurnamesCheckBox.Checked

        Else

            GetPolicy().Identifiers().Surname = Nothing

        End If

    End Sub

    Private Sub URLsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles URLsCheckBox.CheckedChanged

        URLsConfigureButton.Enabled = URLsCheckBox.Checked

        If URLsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Url Is Nothing Then
                GetPolicy().Identifiers().Url = New Filters.Url
            End If

            GetPolicy().Identifiers().Url.Enabled = URLsCheckBox.Checked

        Else

            GetPolicy().Identifiers().Url = Nothing

        End If

    End Sub

    Private Sub VINsCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles VINsCheckBox.CheckedChanged

        VINsConfigureButton.Enabled = VINsCheckBox.Checked

        If VINsCheckBox.Checked = True Then

            If GetPolicy().Identifiers().Vin Is Nothing Then
                GetPolicy().Identifiers().Vin = New Filters.Vin
            End If

            GetPolicy().Identifiers().Vin.Enabled = VINsCheckBox.Checked

        Else

            GetPolicy().Identifiers().Vin = Nothing

        End If

    End Sub

    Private Sub ZipCodesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles ZipCodesCheckBox.CheckedChanged

        ZipCodesConfigureButton.Enabled = ZipCodesCheckBox.Checked

        If ZipCodesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().ZipCode Is Nothing Then
                GetPolicy().Identifiers().ZipCode = New Filters.ZipCode
            End If

            GetPolicy().Identifiers().ZipCode.Enabled = ZipCodesCheckBox.Checked

        Else

            GetPolicy().Identifiers().ZipCode = Nothing

        End If

    End Sub

    Private Sub CustomDictionariesCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CustomDictionariesCheckBox.CheckedChanged

        CustomDictionariesConfigureButton.Enabled = CustomDictionariesCheckBox.Checked

        If CustomDictionariesCheckBox.Checked = True Then

            If GetPolicy().Identifiers().CustomDictionaries Is Nothing Then
                GetPolicy().Identifiers().CustomDictionaries = New List(Of Filters.CustomDictionary)
            End If

            For Each i As CustomDictionary In GetPolicy().Identifiers().CustomDictionaries
                i.Enabled = CustomIdentifiersCheckBox.Checked
            Next

        Else

            GetPolicy().Identifiers().CustomDictionaries = Nothing

        End If

    End Sub

    Private Sub CustomIdentifiersCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CustomIdentifiersCheckBox.CheckedChanged

        CustomIdentifiersConfigureButton.Enabled = CustomIdentifiersCheckBox.Checked

        If CustomIdentifiersCheckBox.Checked = True Then

            If GetPolicy().Identifiers().CustomIdentifiers Is Nothing Then
                GetPolicy().Identifiers().CustomIdentifiers = New List(Of Filters.Identifier)
            End If

            For Each i As Identifier In GetPolicy().Identifiers().CustomIdentifiers
                i.Enabled = CustomIdentifiersCheckBox.Checked
            Next

        Else

            GetPolicy().Identifiers().CustomIdentifiers = Nothing

        End If

    End Sub

    Private Sub CitiesConfigureButton_Click(sender As Object, e As EventArgs) Handles CitiesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.City Is Nothing Then
            Policy.Identifiers.City = New Filters.City
        End If

        Dim fs As New CityFilterStrategiesForm(Policy, City.GetName(), Policy.Identifiers.City.CityFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.City.CityFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.City = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub NERConfigureButton_Click(sender As Object, e As EventArgs) Handles NERConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Ner Is Nothing Then
            Policy.Identifiers.Ner = New Filters.Ner
        End If

        Dim fs As New NerFilterStrategiesForm(Policy, Ner.GetName(), Policy.Identifiers.Ner.nerFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Ner.nerFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Ner = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub CountiesConfigureButton_Click(sender As Object, e As EventArgs) Handles CountiesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.County Is Nothing Then
            Policy.Identifiers.County = New Filters.County
        End If

        Dim fs As New CountyFilterStrategiesForm(Policy, County.GetName(), Policy.Identifiers.County.CountyFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.County.CountyFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.County = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub CreditCardsConfigureButton_Click(sender As Object, e As EventArgs) Handles CreditCardsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.CreditCard Is Nothing Then
            Policy.Identifiers.CreditCard = New Filters.CreditCard
        End If

        Dim fs As New CreditCardFilterStrategiesForm(Policy, CreditCard.GetName(), Policy.Identifiers.CreditCard.CreditCardFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.CreditCard.CreditCardFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.CreditCard = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub DatesConfigureButton_Click(sender As Object, e As EventArgs) Handles DatesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Date Is Nothing Then
            Policy.Identifiers.Date = New Filters.Date
        End If

        Dim fs As New DateFilterStrategiesForm(Policy, [Date].GetName(), Policy.Identifiers.Date.DateFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Date.DateFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Date = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub EmailAddressesConfigureButton_Click(sender As Object, e As EventArgs) Handles EmailAddressesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.EmailAddress Is Nothing Then
            Policy.Identifiers.EmailAddress = New Filters.EmailAddress
        End If

        Dim fs As New EmailAddressFilterStrategiesForm(Policy, EmailAddress.GetName(), Policy.Identifiers.EmailAddress.EmailAddressFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.EmailAddress.EmailAddressFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.EmailAddress = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub FirstNamesConfigureButton_Click(sender As Object, e As EventArgs) Handles FirstNamesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.FirstName Is Nothing Then
            Policy.Identifiers.FirstName = New Filters.FirstName
        End If

        Dim fs As New FirstNameFilterStrategiesForm(Policy, FirstName.GetName(), Policy.Identifiers.FirstName.FirstNameFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.FirstName.FirstNameFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.FirstName = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub HospitalAbbreviationsConfigureButton_Click(sender As Object, e As EventArgs) Handles HospitalAbbreviationsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.HospitalAbbreviation Is Nothing Then
            Policy.Identifiers.HospitalAbbreviation = New Filters.HospitalAbbreviation
        End If

        Dim fs As New HospitalAbbreviationFilterStrategiesForm(Policy, HospitalAbbreviation.GetName(), Policy.Identifiers.HospitalAbbreviation.HospitalAbbreviationFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.HospitalAbbreviation.HospitalAbbreviationFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.HospitalAbbreviation = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub HospitalsConfigureButton_Click(sender As Object, e As EventArgs) Handles HospitalsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Hospital Is Nothing Then
            Policy.Identifiers.Hospital = New Filters.Hospital
        End If

        Dim fs As New HospitalFilterStrategiesForm(Policy, Hospital.GetName(), Policy.Identifiers.Hospital.HospitalFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Hospital.HospitalFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Hospital = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub IPAddressesConfigureButton_Click(sender As Object, e As EventArgs) Handles IPAddressesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.IpAddress Is Nothing Then
            Policy.Identifiers.IpAddress = New Filters.IpAddress
        End If

        Dim fs As New IpAddressFilterStrategiesForm(Policy, IpAddress.GetName(), Policy.Identifiers.IpAddress.IpAddressFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.IpAddress.IpAddressFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.IpAddress = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub PhoneNumbersConfigureButton_Click(sender As Object, e As EventArgs) Handles PhoneNumbersConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.PhoneNumber Is Nothing Then
            Policy.Identifiers.PhoneNumber = New Filters.PhoneNumber
        End If

        Dim fs As New PhoneNumberFilterStrategiesForm(Policy, PhoneNumber.GetName(), Policy.Identifiers.PhoneNumber.PhoneNumberFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.PhoneNumber.PhoneNumberFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.PhoneNumber = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub PhoneNumberExtsConfigureButton_Click(sender As Object, e As EventArgs) Handles PhoneNumberExtsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.PhoneNumberExtension Is Nothing Then
            Policy.Identifiers.PhoneNumberExtension = New Filters.PhoneNumberExtension
        End If

        Dim fs As New PhoneNumberExtensionFilterStrategiesForm(Policy, PhoneNumberExtension.GetName(), Policy.Identifiers.PhoneNumberExtension.PhoneNumberExtensionFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.PhoneNumberExtension.PhoneNumberExtensionFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.PhoneNumberExtension = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub SSNsConfigureButton_Click(sender As Object, e As EventArgs) Handles SSNsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Ssn Is Nothing Then
            Policy.Identifiers.Ssn = New Filters.Ssn
        End If

        Dim fs As New SsnFilterStrategiesForm(Policy, Ssn.GetName(), Policy.Identifiers.Ssn.ssnFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Ssn.ssnFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Ssn = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub StatesConfigureButton_Click(sender As Object, e As EventArgs) Handles StatesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.State Is Nothing Then
            Policy.Identifiers.State = New Filters.State
        End If

        Dim fs As New StateFilterStrategiesForm(Policy, State.GetName(), Policy.Identifiers.State.StateFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.State.StateFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.State = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub StateAbbreviationsConfigureButton_Click(sender As Object, e As EventArgs) Handles StateAbbreviationsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.StateAbbreviation Is Nothing Then
            Policy.Identifiers.StateAbbreviation = New Filters.StateAbbreviation
        End If

        Dim fs As New StateAbbreviationFilterStrategiesForm(Policy, StateAbbreviation.GetName(), Policy.Identifiers.StateAbbreviation.StateAbbreviationFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.StateAbbreviation.StateAbbreviationFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.StateAbbreviation = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub SurnamesConfigureButton_Click(sender As Object, e As EventArgs) Handles SurnamesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Surname Is Nothing Then
            Policy.Identifiers.Surname = New Filters.Surname
        End If

        Dim fs As New SurnameFilterStrategiesForm(Policy, StateAbbreviation.GetName(), Policy.Identifiers.Surname.SurnameFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Surname.SurnameFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Surname = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub URLsConfigureButton_Click(sender As Object, e As EventArgs) Handles URLsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Url Is Nothing Then
            Policy.Identifiers.Url = New Filters.Url
        End If

        Dim fs As New UrlFilterStrategiesForm(Policy, Url.GetName(), Policy.Identifiers.Url.UrlFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Url.UrlFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Url = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub VINsConfigureButton_Click(sender As Object, e As EventArgs) Handles VINsConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.Vin Is Nothing Then
            Policy.Identifiers.Vin = New Filters.Vin
        End If

        Dim fs As New VinFilterStrategiesForm(Policy, Vin.GetName(), Policy.Identifiers.Vin.VinFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.Vin.VinFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.Vin = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub ZipCodesConfigureButton_Click(sender As Object, e As EventArgs) Handles ZipCodesConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.ZipCode Is Nothing Then
            Policy.Identifiers.ZipCode = New Filters.ZipCode
        End If

        Dim fs As New ZipCodeFilterStrategiesForm(Policy, ZipCode.GetName(), Policy.Identifiers.ZipCode.ZipCodeFilterStrategies)

        If fs.ShowDialog = DialogResult.OK Then

            If fs.GetFilterStrategies.Count > 0 Then

                Policy.Identifiers.ZipCode.ZipCodeFilterStrategies = fs.GetFilterStrategies

            Else

                ' No strategies so remove it.
                Policy.Identifiers.ZipCode = Nothing

            End If

        End If

        PolicyPanel.Tag = Policy

        fs.Dispose()

    End Sub

    Private Sub CustomIdentifiersConfigureButton_Click(sender As Object, e As EventArgs) Handles CustomIdentifiersConfigureButton.Click

        Dim Policy As Policy = GetPolicy()

        If Policy.Identifiers.CustomIdentifiers Is Nothing Then
            Policy.Identifiers.CustomIdentifiers = New List(Of Identifier)
        End If

        Dim ci As New CustomIdentifiersForm(Policy.Identifiers.CustomIdentifiers)

        If ci.ShowDialog = DialogResult.OK Then

            Policy.Identifiers.CustomIdentifiers = ci.CustomIdentifiers

        End If

    End Sub

    Private Sub PoliciesToolStripDropDownButton_Click(sender As Object, e As EventArgs) Handles PoliciesToolStripDropDownButton.Click

    End Sub
End Class