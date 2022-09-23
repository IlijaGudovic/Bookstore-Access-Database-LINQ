using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Threading;

namespace Project
{
    public partial class Form1 : Form
    {

        public static Form1 f1;
        private Form2 f2;

        KnjizaraDataSet ds;
        KnjizaraDataSetTableAdapters.KnjigaTableAdapter da;
        KnjizaraDataSetTableAdapters.RacunTableAdapter dss;

        struct bill { public int book_id, cost; }
        List<bill> total = new List<bill>();

        public struct book { public int book_id, times; public string name; }
        public static List<book> sold = new List<book>();

        //Anim
        int xPos = 0, speed = 2, range = 524;
        Thread t;
        List<String> bestsellers;
        int currentBest;

        public Form1()
        {
            InitializeComponent();
            f1 = this;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ds = new KnjizaraDataSet();
            da = new KnjizaraDataSetTableAdapters.KnjigaTableAdapter();

            da.Fill(ds.Knjiga);
            dataGridView1.DataSource = ds.Knjiga;

            loadBookTyps();

            numericUpDown1.Value = 1;
            dataGridView1.AllowUserToAddRows = false;

            sortByName();
            loadBills();

            //Anim
            label6.Font = new System.Drawing.Font(label6.Font.Name, 12F);
            bestsellers = new List<string>();
            updateBestsellers();

            this.Paint += Form1_Paint;
            this.DoubleBuffered = true;

            t = new Thread(animateLabel);
            t.IsBackground = true;
            t.Start();

        }

        //Sort bills by date time
        private void Button1_Click(object sender, EventArgs e)
        {

            DateTime date = dateTimePicker1.Value;
            //MessageBox.Show(date.ToString());

            DataTable filter = ds.Racun.Copy();
            filter.Clear();

            var rows = from row in ds.Racun where row.datum.Year == date.Year && row.datum.Month == date.Month && row.datum.Day == date.Day select row;

            foreach (var row in rows)
            {
                DataRow newRow = filter.NewRow();
                for (int i = 0; i < ds.Racun.Columns.Count; i++)
                    newRow[i] = row[i];

                filter.Rows.Add(newRow);
            }

            dataGridView2.DataSource = filter;

        }

        private void loadBookTyps()
        {
            KnjizaraDataSetTableAdapters.ZanrTableAdapter test = new KnjizaraDataSetTableAdapters.ZanrTableAdapter();
            test.Fill(ds.Zanr);

            var rows = from row in ds.Zanr select row;

            foreach (var row in rows)
            {
                comboBox1.Items.Add(row.naziv);
            }

            if(comboBox1.Items.Count > 0) { comboBox1.SelectedIndex = 0; }

        }

        //Filter books by type
        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox box = (CheckBox)sender;

            if (box.Checked)
            {
                sortBooks();
            }
            else
            {
                dataGridView1.DataSource = ds.Knjiga;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                sortBooks();
            }
        }

        private void sortBooks()
        {
            int selectedType = comboBox1.SelectedIndex + 1;
            if (selectedType == 5)
            {
                selectedType = 6;
            }

            //Find all book IDs by chosen type from data-set Pripadnost
            KnjizaraDataSetTableAdapters.PripadnostTableAdapter dta = new KnjizaraDataSetTableAdapters.PripadnostTableAdapter();
            dta.Fill(ds.Pripadnost);

            List<int> collection = new List<int>();

            var rows1 = from row1 in ds.Pripadnost where row1.id_zanr == selectedType select row1;

            //Store book IDs in collection
            foreach (var row1 in rows1)
            {
                collection.Add(row1.id_knjiga);
            }

            //Find all books in collection from data-set Knjiga
            var rows = from row in ds.Knjiga where collection.Contains(row.id_knjiga) select row;

            //Instantiate new table with colums from data-set Knjiga 
            DataTable filtered = ds.Knjiga.Clone();

            //Fill table with new rows
            foreach (var row in rows)
            {
                DataRow newRow = filtered.NewRow();
                for (int i = 0; i < ds.Knjiga.Columns.Count; i++)
                    newRow[i] = row[i];

                filtered.Rows.Add(newRow);
            }

            dataGridView1.DataSource = filtered;
        }

        //Add selected item to collection
        private void Button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Not selected book");
                return;
            }

            int book_id = (int)dataGridView1.SelectedRows[0].Cells[0].Value;
            string book_name = dataGridView1.SelectedRows[0].Cells[1].Value.ToString();
            int book_cost = (int)dataGridView1.SelectedRows[0].Cells[3].Value;
            int book_discount = (int)dataGridView1.SelectedRows[0].Cells[4].Value;
            int book_amount = (int)numericUpDown1.Value;
            int book_total_cost = (book_cost - (book_cost * book_discount / 100)) * book_amount;

            string bill = "Book " + book_name + " " + book_amount + "x" + book_cost + "din -" + book_discount + "% Total: " + book_total_cost;
            //MessageBox.Show(bill);

            foreach (var item in total)
            {
                if (book_id == item.book_id)
                {
                    MessageBox.Show("Book is alredy on list");
                    return;
                }
            }

            bill newBill; newBill.book_id = book_id; newBill.cost = book_total_cost;
            total.Add(newBill);

            listBox1.Items.Add(bill);

            updateTotal();

        }

        //Remove selected item from collection
        private void Button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("Not selected bill");
                return;
            }

            total.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);

            string cmb = "";
            foreach (var item in total)
            {
                cmb += item.book_id + " " + item.cost + "\n";
            }
            //MessageBox.Show(cmb);

            updateTotal();

        }

        //Remove all items from collection
        private void Button5_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            total.Clear();
            updateTotal();
        }

        //Selecte whole row on cell click
        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Selected = true;
        }

        //Clamp numeric value
        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            if (num.Value < 1)
            {
                num.Value = 1;
            }
        }

        private void updateTotal()
        {
            int total_cost = 0;

            foreach (var item in total)
            {
                total_cost += item.cost;
            }

            textBox1.Text = total_cost.ToString();
        }

        //Checkout
        private void Button6_Click(object sender, EventArgs e)
        {

            if (total.Count == 0)
            {
                return;
            }

            int total_cost = 0;

            foreach (var item in total)
            {
                total_cost += item.cost;
            }

            KnjizaraDataSetTableAdapters.RacunTableAdapter dta = new KnjizaraDataSetTableAdapters.RacunTableAdapter();
            
            int test = dta.Insert(DateTime.Now, total_cost);

            if (test > 0)
            {
                MessageBox.Show("successfully");

                dta.Fill(ds.Racun);
                //dataGridView2.DataSource = ds.Racun;
            }

            //Insert new row in data-set Stavka_racuna 
            foreach (var item in total)
            {
                try
                {
                    DataRow lastRow = ds.Racun.Rows[ds.Racun.Rows.Count - 1]; // ds.Knjiga.Rows.Count

                    KnjizaraDataSetTableAdapters.Stavka_racunaTableAdapter dap = new KnjizaraDataSetTableAdapters.Stavka_racunaTableAdapter();
                    test = dap.Insert((int)lastRow[0], item.book_id, item.cost, 0); //lastRow[0]

                    if (test > 0)
                    {
                        //MessageBox.Show("Dodata stavka: " + (int)lastRow[0] + " " + item.book_id + " " + item.cost);
                        dap.Fill(ds.Stavka_racuna);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            

            //Clear collection
            listBox1.Items.Clear();
            total.Clear();
            updateTotal();
            updateBestsellers();
        }

        //Add new book to data-base
        private void Button7_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(textBoxAutor.Text) || string.IsNullOrEmpty(textBoxNaziv.Text)
                || numericCena.Value == 0 || numericBrStrana.Value == 0)
            {
                MessageBox.Show("Empty field");
                return;
            }

            int test = da.Insert(textBoxAutor.Text, textBoxNaziv.Text, (int)numericCena.Value, (int)numericPopust.Value, (int)numericBrStrana.Value);
            if (test > 0)
            {
                MessageBox.Show("Uspesno dodavanje.");
                da.Fill(ds.Knjiga);
            }

            //Insert new row in data-set Pripadnost
            KnjizaraDataSetTableAdapters.PripadnostTableAdapter dap = new KnjizaraDataSetTableAdapters.PripadnostTableAdapter();

            int selectedType = comboBox1.SelectedIndex + 1;
            if (selectedType == 5)
            {
                selectedType = 6;
            }

            try
            {
                sortById();
                DataRow lastRow = ds.Knjiga.Rows[0]; // ds.Knjiga.Rows.Count
                test = dap.Insert(ds.Knjiga.Rows.Count, selectedType); //lastRow[0]

                if (test > 0)
                {
                    //MessageBox.Show("Uspesno dodavanje pripadnosti. " + ds.Knjiga.Rows.Count + " " + selectedType);
                    dap.Fill(ds.Pripadnost);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        //Save
        private void Button8_Click(object sender, EventArgs e)
        {
            try
            {
                if (da.Update(ds.Knjiga) > 0)
                    MessageBox.Show("Uspesno cuvanje");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                dataGridView1.DataSource = ds.Knjiga;
            }
           
        }

        private void DataGridView1_AllowUserToDeleteRowsChanged(object sender, EventArgs e)
        {
            MessageBox.Show("test");
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.ReadOnly = checkBox2.Checked;
        }

        private void sortById()
        {
            DataTable sorted = ds.Knjiga.Clone();

            var rows = from row in ds.Knjiga orderby row.id_knjiga descending select row;

            foreach (var row in rows)
            {
                DataRow newRow = sorted.NewRow();
                for (int i = 0; i < ds.Knjiga.Columns.Count; i++)
                    newRow[i] = row[i];

                sorted.Rows.Add(newRow);
            }

            dataGridView1.DataSource = sorted;
        }

        private void sortByName()
        {

            dataGridView1.Sort(this.dataGridView1.Columns["Naziv"], ListSortDirection.Ascending);

            return;
            DataTable sorted = ds.Knjiga.Clone();
            //DataTable sorted = ds.Knjiga.Copy();
            //sorted.Clear();

            var rows = from row in ds.Knjiga orderby row.naziv select row;

            foreach (var row in rows)
            {
                DataRow newRow = sorted.NewRow();
                for (int i = 0; i < ds.Knjiga.Columns.Count; i++)
                    newRow[i] = row[i];

                sorted.Rows.Add(newRow);
            }

            dataGridView1.DataSource = sorted;
        }

        private void loadBills()
        {
            dss = new KnjizaraDataSetTableAdapters.RacunTableAdapter();
            dss.Fill(ds.Racun);
            dataGridView2.DataSource = ds.Racun;
        }

        //Sold books
        private void Button2_Click(object sender, EventArgs e)
        {

            updateSoldBooks();

            //Show new form
            f2 = new Form2();
            f2.Show();

        }

        private void updateSoldBooks()
        {
            sold.Clear();

            KnjizaraDataSetTableAdapters.Stavka_racunaTableAdapter dta = new KnjizaraDataSetTableAdapters.Stavka_racunaTableAdapter();
            dta.Fill(ds.Stavka_racuna);

            var rows = from row in ds.Stavka_racuna select row;

            foreach (var row in rows)
            {
                var rows1 = from row1 in ds.Knjiga where row1.id_knjiga == (int)row[1] select row1.naziv; //Select book name by book ID
                List<string> bookNames = rows1.ToList();
                bookNames.Add("null");

                bool contain = false;
                foreach (var book in sold)
                {
                    if (book.book_id == (int)row[1])
                    {
                        contain = true;

                        //replace old book with new data
                        book newBook; newBook.book_id = (int)row[1]; newBook.times = book.times + 1; newBook.name = bookNames[0];
                        sold.Remove(book);
                        sold.Add(newBook);
                        break;
                    }
                }

                if (contain == false)
                {
                    book newBook; newBook.book_id = (int)row[1]; newBook.times = 1; newBook.name = bookNames[0];
                    sold.Add(newBook);
                }

            }

            foreach (var item in sold)
            {
                // MessageBox.Show(item.book_id + " " + item.times + " " + item.name);
            }
        }

        //Reset bills
        private void Button9_Click(object sender, EventArgs e)
        {
            dataGridView2.DataSource = ds.Racun;
        }


        private void animateLabel()
        {
            while (true)
            {
                xPos += speed;

                if (xPos > range)
                {
                    xPos = 0;

                    currentBest++;
                    if (currentBest > bestsellers.Count - 1)
                    {
                        currentBest = 0;
                    }

                }

                Invalidate();
                Thread.Sleep(12);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            label6.Left = xPos;
            label6.Text = bestsellers[currentBest];
        }

        private void updateBestsellers()
        {

            bestsellers.Clear();
            updateSoldBooks();

            sold.Sort((s1, s2) => s2.times.CompareTo(s1.times));

            int counter = 0;

            foreach (var item in sold)
            {
                if (counter > 2)
                {
                    break;
                }
                counter++;
                bestsellers.Add(item.name);
            }

            if (counter == 0)
            {
                bestsellers.Add("Bestsellers");
            }

        }

    }
}
