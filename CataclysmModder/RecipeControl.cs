﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CataclysmModder
{
    public partial class RecipeControl : UserControl
    {
        private class ComponentGroup : INotifyPropertyChanged
        {
            public BindingList<ItemGroupLine> items = new BindingList<ItemGroupLine>();

            public event PropertyChangedEventHandler PropertyChanged;

            public bool isTools = false;

            public string Display
            {
                get
                {
                    if (items.Count == 1)
                        return items[0].Display;
                    else if (items.Count > 1)
                        return items[0].Display + " etc.";
                    else
                        return "empty";
                }
            }

            public void NotifyItemChanged(object sender, PropertyChangedEventArgs args)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Display"));
            }

            public ComponentGroup()
            {
                AddNew();
            }

            public ComponentGroup(object[] inItems)
            {
                foreach (object[] data in inItems)
                {
                    items.Add(new ItemGroupLine(data));
                    items[items.Count - 1].PropertyChanged += NotifyItemChanged;
                }
            }

            public void AddNew()
            {
                items.Add(new ItemGroupLine());
                items[items.Count - 1].PropertyChanged += NotifyItemChanged;
            }
        }

        private ComponentGroup SelectedGroup
        {
            get
            {
                if (toolsListBox.SelectedItem != null)
                    return (ComponentGroup)toolsListBox.SelectedItem;
                else if (componentsListBox.SelectedItem != null)
                    return (ComponentGroup)componentsListBox.SelectedItem;
                else
                    return null;
            }
        }

        private BindingList<ComponentGroup> toolGroups = new BindingList<ComponentGroup>();
        private BindingList<ComponentGroup> componentGroups = new BindingList<ComponentGroup>();

        private BindingList<ItemGroupLine> bookGroup = new BindingList<ItemGroupLine>();

        public RecipeControl()
        {
            InitializeComponent();

            resultTextBox.Tag = new JsonFormTag(
                "result",
                "The id of the item this recipe produces.");
            ((JsonFormTag)resultTextBox.Tag).isItemId = true;
            suffixTextBox.Tag = new JsonFormTag(
                "id_suffix",
                "You need to set this to a unique value if multiple recipes produce the same item.",
                false);
            skill1ComboBox.Tag = new JsonFormTag(
                "skill_pri",
                "The main skill used in crafting this recipe.",
                false);
            skill2ComboBox.Tag = new JsonFormTag(
                "skill_sec",
                "A secondary skill used in crafting this recipe.",
                false);
            diffNumeric.Tag = new JsonFormTag(
                "difficulty",
                "The skill level required to craft this recipe.");
            timeNumeric.Tag = new JsonFormTag(
                "time",
                "The amount of time required to craft this recipe (in seconds?).");
            categoryComboBox.Tag = new JsonFormTag(
                "category",
                "The tab this recipe appears under in the crafting menu.");
            autolearnCheckBox.Tag = new JsonFormTag(
                "autolearn",
                "Is this recipe automatically learned at the appropriate skill level?",
                true,
                true);
            reversibleCheckBox.Tag = new JsonFormTag(
                "reversible",
                "Can this recipe be used to take the result item apart?",
                false,
                false);
            disLearnNumeric.Tag = new JsonFormTag(
                "decomp_learn",
                "The skill level required to learn this recipe by disassembling the result item (-1 forbids this).",
                false,
                -1);

            //Fields that aren't saved directly
            toolsListBox.Tag = new JsonFormTag(
                null,
                "A list of tool items required to craft this recipe.");
            componentsListBox.Tag = new JsonFormTag(
                null,
                "A list of ingredients needed to craft this recipe.");
            itemsListBox.Tag = new JsonFormTag(
                null,
                "A list of interchangeable items used by this particular component or tool group.");
            itemIdTextField.Tag = new JsonFormTag(
                null,
                "The string id of this item.");
            ((JsonFormTag)itemIdTextField.Tag).isItemId = true;
            quantityNumeric.Tag = new JsonFormTag(
                null,
                "For components, the quantity used. For tools, the number of charges used (-1 for no charges).");
            booksListBox.Tag = new JsonFormTag(
                null,
                "A list of books that this recipe might be learned from.");
            bookIdTextBox.Tag = new JsonFormTag(
                null,
                "The string id of the book.");
            ((JsonFormTag)bookIdTextBox.Tag).isBookId = true;
            bookReqLevelNumeric.Tag = new JsonFormTag(
                null,
                "The level required before this recipe can be learned from this book.");

            WinformsUtil.ControlsAttachHooks(Controls[0]);
            WinformsUtil.TagsSetDefaults(Controls[0]);

            toolsListBox.DataSource = toolGroups;
            toolsListBox.DisplayMember = "Display";

            componentsListBox.DataSource = componentGroups;
            componentsListBox.DisplayMember = "Display";

            booksListBox.DataSource = bookGroup;
            booksListBox.DisplayMember = "Display";

            Form1.Instance.ReloadLists += LoadLists;
            WinformsUtil.OnReset += Reset;
            WinformsUtil.OnLoadItem += LoadItem;
        }

        private void LoadLists()
        {
            Storage.LoadCraftCategories(categoryComboBox);
            Storage.LoadSkills(skill1ComboBox);
            Storage.LoadSkills(skill2ComboBox);
        }

        private void Reset()
        {
            WinformsUtil.Resetting++;
            itemIdTextField.Text = "";
            quantityNumeric.Value = 0;
            toolGroups.Clear();
            componentGroups.Clear();
            bookGroup.Clear();
            itemsListBox.DataSource = null;
            WinformsUtil.Resetting--;
        }

        private void LoadItem(object item)
        {
            Dictionary<string, object> dict = (Dictionary<string, object>)item;

            //Load tools
            toolGroups.Clear();
            if (dict.ContainsKey("tools"))
                foreach (object[] data in (object[])dict["tools"])
                    toolGroups.Add(new ComponentGroup(data));

            //Load components
            componentGroups.Clear();
            if (dict.ContainsKey("components"))
                foreach (object[] data in (object[])dict["components"])
                    componentGroups.Add(new ComponentGroup(data));

            //Load books
            bookGroup.Clear();
            if (dict.ContainsKey("book_learn"))
                foreach (object[] data in (object[])dict["book_learn"])
                    bookGroup.Add(new ItemGroupLine(data));

            //Select none
            toolsListBox.SelectedIndex = -1;
            componentsListBox.SelectedIndex = -1;
            booksListBox.SelectedIndex = -1;
        }

        private void toolsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (toolsListBox.SelectedItem != null)
            {
                componentsListBox.SelectedIndex = -1;
                //itemsListBox.SelectedIndex = -1;

                itemsListBox.DataSource = ((ComponentGroup)toolsListBox.SelectedItem).items;
                itemsListBox.DisplayMember = "Display";
            }
        }

        private void componentsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (componentsListBox.SelectedItem != null)
            {
                toolsListBox.SelectedIndex = -1;
                //itemsListBox.SelectedIndex = -1;

                itemsListBox.DataSource = ((ComponentGroup)componentsListBox.SelectedItem).items;
                itemsListBox.DisplayMember = "Display";
            }
        }

        private bool changing = false;

        private void itemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            WinformsUtil.Resetting++;
            if (itemsListBox.SelectedItem != null)
            {
                itemIdTextField.Enabled = true;
                quantityNumeric.Enabled = true;
                itemIdTextField.Text = ((ItemGroupLine)itemsListBox.SelectedItem).Id;
                quantityNumeric.Value = ((ItemGroupLine)itemsListBox.SelectedItem).Value;
            }
            else if (!changing)
            {
                ClearItem();
            }
            WinformsUtil.Resetting--;
        }

        private void ClearItem()
        {
            itemIdTextField.Enabled = false;
            quantityNumeric.Enabled = false;
            WinformsUtil.Resetting++;
            itemIdTextField.Text = "";
            quantityNumeric.Value = 0;
            WinformsUtil.Resetting--;
        }

        private void ClearItems()
        {
            itemsListBox.DataSource = null;
        }

        private void SaveItemlistToStorage()
        {
            if (toolsListBox.SelectedItem != null)
                SaveToolListToStorage();
            if (componentsListBox.SelectedItem != null)
                SaveComponentListToStorage();
        }

        private void SaveComponentListToStorage()
        {
            object[] cgroups = new object[componentGroups.Count];
            int c = 0;
            foreach (ComponentGroup cg in componentGroups)
            {
                object[] cgroup = new object[cg.items.Count];
                int d = 0;
                foreach (ItemGroupLine cgi in cg.items)
                {
                    cgroup[d] = (object[])cgi;
                    d++;
                }
                cgroups[c] = cgroup;
                c++;
            }
            Storage.ItemApplyValue("components", cgroups, true);
        }

        private void SaveToolListToStorage()
        {
            object[] cgroups = new object[toolGroups.Count];
            int c = 0;
            foreach (ComponentGroup cg in toolGroups)
            {
                object[] cgroup = new object[cg.items.Count];
                int d = 0;
                foreach (ItemGroupLine cgi in cg.items)
                {
                    cgroup[d] = (object[])cgi;
                    d++;
                }
                cgroups[c] = cgroup;
                c++;
            }
            Storage.ItemApplyValue("tools", cgroups, true);
        }

        private void itemIdTextField_TextChanged(object sender, EventArgs e)
        {
            if (WinformsUtil.Resetting > 0) return;

            changing = true;
            ((ItemGroupLine)itemsListBox.SelectedItem).Id = itemIdTextField.Text;
            changing = false;
            SaveItemlistToStorage();
        }

        private void quantityNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (WinformsUtil.Resetting > 0) return;

            changing = true;
            ((ItemGroupLine)itemsListBox.SelectedItem).Value = (int)quantityNumeric.Value;
            changing = false;
            SaveItemlistToStorage();
        }

        private void newItemButton_Click(object sender, EventArgs e)
        {
            if (SelectedGroup != null)
            {
                SelectedGroup.AddNew();
                itemsListBox.SelectedIndex = itemsListBox.Items.Count - 1;
            }
        }

        private void deleteItemButton_Click(object sender, EventArgs e)
        {
            if (SelectedGroup != null)
            {
                SelectedGroup.items.RemoveAt(itemsListBox.SelectedIndex);
                if (SelectedGroup.items.Count == 0)
                    ClearItem();
                itemsListBox_SelectedIndexChanged(null, null);
            }
        }

        private void newToolButton_Click(object sender, EventArgs e)
        {
            toolGroups.Add(new ComponentGroup());
            toolsListBox.SelectedIndex = toolsListBox.Items.Count - 1;
            toolsListBox_SelectedIndexChanged(null, null);
            SaveToolListToStorage();
        }

        private void deleteToolButton_Click(object sender, EventArgs e)
        {
            if (toolsListBox.SelectedItem != null)
            {
                toolGroups.Remove((ComponentGroup)toolsListBox.SelectedItem);
                if (toolGroups.Count == 0)
                {
                    ClearItem();
                    ClearItems();
                }
                toolsListBox_SelectedIndexChanged(null, null);
                SaveToolListToStorage();
            }
        }

        private void newComponentButton_Click(object sender, EventArgs e)
        {
            componentGroups.Add(new ComponentGroup());
            componentsListBox.SelectedIndex = componentsListBox.Items.Count - 1;
            componentsListBox_SelectedIndexChanged(null, null);
            SaveComponentListToStorage();
        }

        private void deleteComponentButton_Click(object sender, EventArgs e)
        {
            if (componentsListBox.SelectedItem != null)
            {
                componentGroups.Remove((ComponentGroup)componentsListBox.SelectedItem);
                if (componentGroups.Count == 0)
                {
                    ClearItem();
                    ClearItems();
                }
                componentsListBox_SelectedIndexChanged(null, null);
                SaveComponentListToStorage();
            }
        }

        private void booksListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            WinformsUtil.Resetting++;
            if (booksListBox.SelectedItem != null)
            {
                bookIdTextBox.Enabled = true;
                bookReqLevelNumeric.Enabled = true;
                bookIdTextBox.Text = ((ItemGroupLine)booksListBox.SelectedItem).Id;
                bookReqLevelNumeric.Value = ((ItemGroupLine)booksListBox.SelectedItem).Value;
            }
            else if (!changing)
            {
                bookIdTextBox.Enabled = false;
                bookReqLevelNumeric.Enabled = false;
                bookIdTextBox.Text = "";
                bookReqLevelNumeric.Value = 0;
            }
            WinformsUtil.Resetting--;
        }

        private void newBook_Click(object sender, EventArgs e)
        {
            bookGroup.Add(new ItemGroupLine());
            booksListBox.SelectedIndex = booksListBox.Items.Count - 1;
            booksListBox_SelectedIndexChanged(null, null);
            SaveBookListToStorage();
        }

        private void deleteBook_Click(object sender, EventArgs e)
        {
            if (booksListBox.SelectedItem != null)
            {
                bookGroup.Remove((ItemGroupLine)booksListBox.SelectedItem);
                booksListBox_SelectedIndexChanged(null, null);
                SaveBookListToStorage();
            }
        }

        private void bookIdTextBox_TextChanged(object sender, EventArgs e)
        {
            if (WinformsUtil.Resetting > 0) return;

            changing = true;
            ((ItemGroupLine)booksListBox.SelectedItem).Id = bookIdTextBox.Text;
            changing = false;
            SaveItemlistToStorage();
        }

        private void bookReqLevelNumeric_ValueChanged(object sender, EventArgs e)
        {
            if (WinformsUtil.Resetting > 0) return;

            changing = true;
            ((ItemGroupLine)booksListBox.SelectedItem).Value = (int)bookReqLevelNumeric.Value;
            changing = false;
            SaveItemlistToStorage();
        }

        private void SaveBookListToStorage()
        {
            WinformsUtil.ApplyItemGroupLines("book_learn", bookGroup, false);
        }
    }
}
