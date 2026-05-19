using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace lab_666
{
    public partial class Form1 : Form
    {
        private string _currentFilePath = "";

        public Form1()
        {
            InitializeComponent();
            txtInput.AllowDrop = true;
            txtInput.DragEnter += TxtInput_DragEnter;
            txtInput.DragDrop += TxtInput_DragDrop;
            dgvResults.ReadOnly = true;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.CellClick += dgvResults_CellClick;
        }

        private bool AskToSave()
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
                return true;

            DialogResult result = MessageBox.Show(
                "Сохранить изменения?",
                "Подтверждение",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveButton();
                return true;
            }
            else if (result == DialogResult.No)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // ==================== ОСНОВНОЙ МЕТОД ЗАПУСКА ====================
        private void StartButton()
        {
            dgvResults.Rows.Clear();
            string input = txtInput.Text;

            if (string.IsNullOrWhiteSpace(input))
            {
                dgvResults.Rows.Add("", "Введите код для анализа");
                return;
            }

            // 1. Лексический анализ
            var tokens = Lexer.Tokenize(input);
            var lexErrors = Lexer.GetErrors();

            foreach (var err in lexErrors)
            {
                dgvResults.Rows.Add($"строка {err.Line}, позиция {err.Position}", $"Лексическая ошибка: {err.Description}");
            }

            if (lexErrors.Count > 0)
            {
                dgvResults.Rows.Add("", $"Всего лексических ошибок: {lexErrors.Count}");
                return;
            }

            dgvResults.Rows.Add("", "✓ Лексических ошибок не найдено");

            // 2. Синтаксический анализ
            Parser parser = new Parser(tokens);
            AstNode ast = parser.Parse();

            foreach (var err in parser.Errors)
            {
                dgvResults.Rows.Add($"строка {err.Line}, позиция {err.Position}", $"Синтаксическая ошибка: {err.Description}");
            }

            if (parser.Errors.Count > 0)
            {
                dgvResults.Rows.Add("", $"Всего синтаксических ошибок: {parser.Errors.Count}");
                return;
            }

            dgvResults.Rows.Add("", "✓ Синтаксических ошибок не найдено");

            string astOutput = null;

            if (ast != null)
            {
                // 3. Семантический анализ и генерация тетрад
                var semanticAnalyzer = new SemanticAnalyzer();
                var validatedAst = semanticAnalyzer.Analyze(ast);

                var sortedErrors = semanticAnalyzer.Errors
                    .OrderBy(err => err.Line)
                    .ThenBy(err => err.Column)
                    .ToList();

                if (validatedAst is BlockNode block && block.Statements.Count > 0)
                {
                    astOutput = "AST (синтаксическое дерево)\n\n";

                    for (int i = 0; i < block.Statements.Count; i++)
                    {
                        var stmt = block.Statements[i];
                        bool hasErrorForThisStmt = false;

                        if (stmt is AssignNode assignStmt)
                        {
                            foreach (var err in sortedErrors)
                            {
                                if (err.Line == assignStmt.Line && err.Column == assignStmt.Column)
                                {
                                    hasErrorForThisStmt = true;
                                    break;
                                }
                            }
                        }

                        if (!hasErrorForThisStmt)
                        {
                            astOutput += $"--- Строка {stmt.Line} ---\n";
                            astOutput += stmt.ToTree();
                            astOutput += "\n";
                        }
                    }
                }

                foreach (var err in sortedErrors)
                {
                    dgvResults.Rows.Add($"строка {err.Line}, позиция {err.Column}", $"Семантическая ошибка: {err.Message}");
                }

                if (semanticAnalyzer.Errors.Count == 0)
                {
                    dgvResults.Rows.Add("", "✓ Семантических ошибок не найдено");

                    // 4. ВЫВОД ТЕТРАД
                    var quads = semanticAnalyzer.GetQuads();

                    // ВЫВОД В ТАБЛИЦУ tetrads
                    DisplayTetradsInTable(quads);

                    // 5. ПОЛИЗ (только для выражений с числами) - ИЗМЕНЕННАЯ ЧАСТЬ
                    bool hasVariables = tokens.Any(t => t.Type == "IDENT");

                    if (!hasVariables)
                    {
                        try
                        {
                            var calc = new RPNCalculator();
                            var rpn = calc.ToRPN(input);
                            var result = calc.EvaluateRPN(rpn);

                            // ВЫВОД В MESSAGEBOX
                            MessageBox.Show($"Выражение: {input}\n\nПОЛИЗ: {string.Join(" ", rpn)}\n\nРезультат: {result}",
                                            "Результат вычислений",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка ПОЛИЗ: {ex.Message}",
                                            "Ошибка",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    dgvResults.Rows.Add("", $"Всего семантических ошибок: {semanticAnalyzer.Errors.Count}");
                }
            }

            if (!string.IsNullOrEmpty(astOutput))
            {
                MessageBox.Show(astOutput, "Абстрактное синтаксическое дерево (AST)",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Добавьте этот метод в класс Form1 (перед методом StartButton или после него)
        private void DisplayTetradsInTable(List<Quadruple> quads)
        {
            // Очищаем таблицу
            tetrads.Rows.Clear();

            if (quads == null || quads.Count == 0)
            {
                tetrads.Rows.Add("", "Нет тетрад", "", "", "");
                return;
            }

            // Проверяем наличие колонок, если нет - создаем
            if (tetrads.Columns.Count == 0)
            {
                tetrads.Columns.Add("Num", "№");
                tetrads.Columns.Add("Op", "Операция");
                tetrads.Columns.Add("Arg1", "Аргумент 1");
                tetrads.Columns.Add("Arg2", "Аргумент 2");
                tetrads.Columns.Add("Result", "Результат");

                tetrads.Columns["Num"].Width = 50;
                tetrads.Columns["Op"].Width = 80;
                tetrads.Columns["Arg1"].Width = 100;
                tetrads.Columns["Arg2"].Width = 100;
                tetrads.Columns["Result"].Width = 100;

                tetrads.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
                tetrads.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                tetrads.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                tetrads.DefaultCellStyle.Font = new Font("Consolas", 9);
                tetrads.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Заполняем таблицу
            for (int i = 0; i < quads.Count; i++)
            {
                int rowIndex = tetrads.Rows.Add();
                tetrads.Rows[rowIndex].Cells["Num"].Value = (i + 1).ToString();
                tetrads.Rows[rowIndex].Cells["Op"].Value = quads[i].Op;
                tetrads.Rows[rowIndex].Cells["Arg1"].Value = quads[i].Arg1;
                tetrads.Rows[rowIndex].Cells["Arg2"].Value = quads[i].Arg2;
                tetrads.Rows[rowIndex].Cells["Result"].Value = quads[i].Result;
            }
        }
        // ==================== ВСЕ ВАШИ СУЩЕСТВУЮЩИЕ МЕТОДЫ ====================
        private void OpenButton()
        {
            if (!AskToSave())
                return;

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                txtInput.Text = System.IO.File.ReadAllText(openFile.FileName);
                _currentFilePath = openFile.FileName;
            }
        }

        private void AddButton()
        {
            if (AskToSave())
            {
                txtInput.Text = "";
                _currentFilePath = "";
            }
        }

        private void SaveButton()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsButton();
            }
            else
            {
                try
                {
                    System.IO.File.WriteAllText(_currentFilePath, txtInput.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveAsButton()
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*";

            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(saveFile.FileName, txtInput.Text);
                _currentFilePath = saveFile.FileName;
            }
        }

        private void CopyButton()
        {
            if (txtInput.SelectedText != "")
            {
                Clipboard.SetText(txtInput.SelectedText);
            }
        }

        private void InsertButton()
        {
            if (Clipboard.ContainsText())
            {
                txtInput.Text = txtInput.Text + Clipboard.GetText();
            }
        }

        private void CutButton()
        {
            if (txtInput.SelectedText != "")
            {
                Clipboard.SetText(txtInput.SelectedText);
                int selectionStart = txtInput.SelectionStart;
                int selectionLength = txtInput.SelectionLength;
                txtInput.Text = txtInput.Text.Remove(selectionStart, selectionLength);
                txtInput.SelectionStart = selectionStart;
            }
        }

        private void CancelButton()
        {
            if (txtInput.CanUndo)
            {
                txtInput.Undo();
            }
        }

        private void RepeatButton()
        {
            if (txtInput.CanRedo)
            {
                txtInput.Redo();
            }
        }

        // ==================== ОБРАБОТЧИКИ КНОПОК ====================
        private void StartButton_Click(object sender, EventArgs e)
        {
            StartButton();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddButton();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenButton();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveAsButton();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            CopyButton();
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            InsertButton();
        }

        private void btnCut_Click(object sender, EventArgs e)
        {
            CutButton();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            CancelButton();
        }

        private void btnRepeat_Click(object sender, EventArgs e)
        {
            RepeatButton();
        }

        private void btnSize_ValueChanged(object sender, EventArgs e)
        {
            float newSize = (float)btnSize.Value;
            txtInput.Font = new Font(txtInput.Font.FontFamily, newSize, txtInput.Font.Style);
            txtOutput.Font = new Font(txtOutput.Font.FontFamily, newSize, txtOutput.Font.Style);
        }

        private void TxtInput_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void TxtInput_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                txtInput.Text = System.IO.File.ReadAllText(files[0]);
                _currentFilePath = files[0];
            }
        }

        private void btnEnglish_Click(object sender, EventArgs e)
        {
            btnStart.Text = "Run";
            btnAdd.Text = "New";
            btnOpen.Text = "Open";
            btnSave.Text = "Save";
            btnCopy.Text = "Copy";
            btnInsert.Text = "Paste";
            btnCut.Text = "Cut";
            btnCancel.Text = "Cancel";
            btnRepeat.Text = "Repeat";
            Exit.Text = "Exit";
            File.Text = "File";
            menuAdd.Text = "Create";
            menuOpen.Text = "Open";
            menuSave.Text = "Save";
            menuSaveAs.Text = "Save as";
            Edit.Text = "Editing";
            menuCancel.Text = "Cancel";
            menuRepeat.Text = "Repeat";
            menuCut.Text = "Cut";
            menuCopy.Text = "Copy";
            menuInsert.Text = "Insert";
            menuDelete.Text = "Delete";
            menuDeleteAll.Text = "Delete all";
            Start.Text = "Start";
            Reference.Text = "Reference";
            menuReference.Text = "Call for help";
            menuAbout.Text = "About program";
            Language.Text = "Language";
            Font.Text = "Font size";
        }

        private void btnRussian_Click(object sender, EventArgs e)
        {
            btnStart.Text = "Запуск";
            btnAdd.Text = "Новый";
            btnOpen.Text = "Открыть";
            btnSave.Text = "Сохранить";
            btnCopy.Text = "Копировать";
            btnInsert.Text = "Вставить";
            btnCut.Text = "Вырезать";
            btnCancel.Text = "Отменить";
            btnRepeat.Text = "Повторить";
            Exit.Text = "Выход";
            File.Text = "Файл";
            menuAdd.Text = "Создать";
            menuOpen.Text = "Открыть";
            menuSave.Text = "Сохранить";
            menuSaveAs.Text = "Сохранить как";
            Edit.Text = "Правка";
            menuCancel.Text = "Отмена";
            menuRepeat.Text = "Возврат";
            menuCut.Text = "Вырезать";
            menuCopy.Text = "Копировать";
            menuInsert.Text = "Вставить";
            menuDelete.Text = "Удалить";
            menuDeleteAll.Text = "Удалить все";
            Start.Text = "Пуск";
            Reference.Text = "Справка";
            menuReference.Text = "Вызов справки";
            menuAbout.Text = "О программе";
            Language.Text = "Язык";
            Font.Text = "Размер шрифта";
        }

        // ==================== ОБРАБОТЧИКИ МЕНЮ ====================
        private void menuAdd_Click(object sender, EventArgs e)
        {
            AddButton();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            OpenButton();
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            SaveButton();
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            SaveAsButton();
        }

        private void menuCancel_Click(object sender, EventArgs e)
        {
            CancelButton();
        }

        private void menuRepeat_Click(object sender, EventArgs e)
        {
            RepeatButton();
        }

        private void menuCut_Click(object sender, EventArgs e)
        {
            CutButton();
        }

        private void menuCopy_Click(object sender, EventArgs e)
        {
            CopyButton();
        }

        private void menuInsert_Click(object sender, EventArgs e)
        {
            InsertButton();
        }

        private void menuDelete_Click(object sender, EventArgs e)
        {
            if (txtInput.SelectedText != "")
            {
                int start = txtInput.SelectionStart;
                int length = txtInput.SelectionLength;
                txtInput.Text = txtInput.Text.Remove(start, length);
                txtInput.SelectionStart = start;
            }
        }

        private void menuDeleteAll_Click(object sender, EventArgs e)
        {
            txtInput.Text = "";
        }

        private void Start_Click(object sender, EventArgs e)
        {
            StartButton();
        }

        private void menuReference_Click_1(object sender, EventArgs e)
        {
            string helpText =
                "Описание функций приложения\n\n" +
                "═══════════════════════════════════════════════════════\n\n" +
                "ГРАММАТИКА:\n" +
                "E → TA\n" +
                "A → ε | +TA | -TA\n" +
                "T → FB\n" +
                "B → ε | *FB | /FB | //FB | %FB | **FB\n" +
                "F → num | id | (E)\n\n" +
                "ОСНОВНЫЕ ФУНКЦИИ:\n" +
                "• Запуск - лексический, синтаксический, семантический анализ\n" +
                "• Генерация тетрад и ПОЛИЗ\n" +
                "• Вычисление арифметических выражений\n\n" +
                "РАБОТА С ФАЙЛАМИ:\n" +
                "• Создать/Открыть/Сохранить/Сохранить как\n\n" +
                "РЕДАКТИРОВАНИЕ:\n" +
                "• Отменить/Повторить/Вырезать/Копировать/Вставить\n" +
                "• Удалить/Удалить все\n\n" +
                "ДОПОЛНИТЕЛЬНО:\n" +
                "• Изменение размера шрифта\n" +
                "• Смена языка интерфейса";

            MessageBox.Show(helpText, "Справка по функциям",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Компилятор арифметических выражений\nВерсия 2.0\n\n" +
                "Лабораторная работа №6\n" +
                "Внутреннее представление программы\n\n" +
                "Разработано для учебных целей",
                "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInput.Text))
            {
                DialogResult result = MessageBox.Show(
                    "Вы действительно хотите выйти из приложения?",
                    "Подтверждение выхода",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void dgvResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string loc = dgvResults.Rows[e.RowIndex].Cells[0].Value?.ToString();

            if (string.IsNullOrEmpty(loc) || loc.Contains("Всего ошибок") || loc.Contains("не найдено") || loc.Contains("═══"))
            {
                return;
            }

            try
            {
                var parts = loc.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 4 && parts[0] == "строка" && parts[2] == "позиция")
                {
                    int line = int.Parse(parts[1]);
                    int pos = int.Parse(parts[3]);

                    int index = GetIndex(line, pos);

                    txtInput.Focus();
                    txtInput.SelectionStart = index;
                    txtInput.SelectionLength = 1;
                }
            }
            catch
            {
            }
        }

        private int GetIndex(int line, int pos)
        {
            int currentLine = 1;
            int index = 0;

            foreach (char c in txtInput.Text)
            {
                if (currentLine == line)
                    break;

                if (c == '\n')
                    currentLine++;

                index++;
            }

            return index + pos - 1;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartButton();
        }

        private void Start_Click_1(object sender, EventArgs e)
        {
            StartButton();
        }

        private void txtInput_TextChanged(object sender, EventArgs e)
        {

        }
    }
}