using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClashRoyaleDeckGenerator
{
    public partial class Form1 : Form
    {

        private static String connectionString = @"Data Source = localhost; Integrated Security = True";    // Database connection string
        private static String sqlString = "select * from ClashRoyale.dbo.CardList";                         // SQL statement
        private static ArrayList myList = new ArrayList();              // Array list containing the card IDs for the deck
        private static ArrayList exclusionList = new ArrayList();       // Array list containing cards higher than the arena level selected
        private static int arenaLevel;                                  // Arena level, used in the combo box
        private static Boolean newDeck = true;                          // Used for generating a new deck or changing a single card

        public Form1()
        {
            InitializeComponent();

            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();

            // Makes the data grid view read only
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Pulls the arena levels and populates it into the combo box
            string Sql = "select distinct Arena from ClashRoyale.dbo.CardList";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand cmd = new SqlCommand(Sql, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                comboBox1.Items.Add(dr[0]);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.cardListTableAdapter.Fill(this.clashRoyaleDataSet.CardList);
        }

        // Generates a new deck, connects to the database
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                exclusionList = new ArrayList();
                if (newDeck)
                {
                    myList = new ArrayList();
                    sqlString = "select * from ClashRoyale.dbo.CardList";
                }
                comboBox1_SelectedIndexChanged(sender, e);
                SqlConnection conn = new SqlConnection(connectionString);
                addToExcluded();
                if (newDeck)
                    generateNumbers();
                appendSqlString();
                conn.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(sqlString, conn);
                DataSet ds = new DataSet();
                dataAdapter.Fill(ds);
                dataGridView1.DataSource = ds.Tables[0];
                conn.Close();                
            }
            else
            {
                MessageBox.Show("ERROR: Null value in Arena Level field.");
            }
            double avg = 0;
            for (int rowIndex = 0; rowIndex < dataGridView1.RowCount; rowIndex++)
            {
                double num;
                bool result = Double.TryParse(dataGridView1[3, rowIndex].Value.ToString(), out num);
                avg += num;
            }
            if (myList.IndexOf(44) == -1)
                avg /= 8;
            else
                avg /= 7;
            textBox1.Text = Math.Round(avg, 1).ToString(".0#");
        }

        // Generates unique 8 numbers and stores them in the array list 
        private static void generateNumbers()
        {
            Random rand = new Random();
            for (int i = 0; i < 8; i++)
            {
                int r = rand.Next(1, 79);
                if (exclusionList.IndexOf(r) != -1)
                    i--;
                else if (myList.IndexOf(r) == -1)
                    myList.Add(r);
                else
                    i--;
            }
            myList.Sort();
        }

        // Excludes arena level cards higher than the selected level
        private static void addToExcluded()
        {
            string Sql = "select distinct ID from ClashRoyale.dbo.CardList where Arena >" + arenaLevel;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand cmd = new SqlCommand(Sql, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                exclusionList.Add(dr[0]);
            }
        }

        // Turns the array into a string  
        private static void appendSqlString()
        {
		    String s1 = " where ID in ( ";
            foreach (Object o in myList) {
               s1 += o + ", ";
            }
            s1 += ")";
            String s2 = s1.Replace(", )", " )");
            sqlString += s2;
	    }

        // Drop down combo box for the arena level
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            arenaLevel = Int32.Parse(comboBox1.Text);
            addToExcluded();
            sqlString = "select * from ClashRoyale.dbo.CardList";
        }

        // Changes the card of the selected row in the datagrid to a new card, keeping the other 7 cards
        private void btnSwitch_Click(object sender, EventArgs e)
        {
            if ((dataGridView1.SelectedRows != null) &&
                (dataGridView1.SelectedRows.Count > 0) &&
                dataGridView1.SelectedRows[0].Cells[0].Value != null)
            {
                int newCard = Int32.Parse(dataGridView1.SelectedRows[0].Cells[0].Value.ToString());
                myList.Remove(newCard);

                Random rand = new Random();
                int iterator = 0;
                while (iterator != 1)
                {
                    int r = rand.Next(1, 79);
                    if (myList.IndexOf(r) == -1 && exclusionList.IndexOf(r) == -1 && r != newCard)
                    {
                        myList.Add(r);
                        iterator++;
                    }
                }
            }
            else
            {
                MessageBox.Show("ERROR: No row selected in the data grid view.");
            }
            newDeck = false;
            btnGenerate_Click(sender, e);
            newDeck = true;
        }

        // Exports the values in the data grid view to a .csv file
        private void btnExport_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            var headers = dataGridView1.Columns.Cast<DataGridViewColumn>();
            sb.AppendLine(string.Join(",", headers.Select(column => "\"" + column.HeaderText + "\"").ToArray()));
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var cells = row.Cells.Cast<DataGridViewCell>();
                sb.AppendLine(string.Join(",", cells.Select(cell => "\"" + cell.Value + "\"").ToArray()));
            }
            System.IO.File.WriteAllText(@"Deck.csv", sb.ToString());
            MessageBox.Show("Your deck has been exported to a csv file.");
        }
    }
}
