using AvaloniaManager.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Documents;
using System.Windows;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AvaloniaManager.Services
{
    public class ReportService
    {
        public async Task GenerateArticlesReport(List<Article> articles, string reportTitle = "Отчет по статьям")
        {
            if (articles == null || articles.Count == 0)
            {
                await DialogService.ShowErrorNotification("Нет данных для отчета");
                return;
            }

            try
            {
                // Выбор места сохранения 
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Документ Word (*.docx)|*.docx",
                    FileName = $"{reportTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.docx",
                    Title = "Сохранить отчет",
                    DefaultExt = ".docx"
                };

                var filePath = saveDialog.ShowDialog() == true ? saveDialog.FileName : null;
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                // Генерация документа
                using (var wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    Body body = new Body();

                    // Крафт заголовка
                    Paragraph title = new Paragraph(
                        new Run(new Text(reportTitle))
                        {
                            RunProperties = new RunProperties(
                                new Bold(),
                                new FontSize { Val = "32" },
                                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
                            )
                        }
                    );
                    title.ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center });
                    body.Append(title);

                    // Дата
                    Paragraph dateParagraph = new Paragraph(
                        new Run(new Text($"Дата формирования: {DateTime.Now:dd.MM.yyyy}"))
                        {
                            RunProperties = new RunProperties(
                                new FontSize { Val = "24" },
                                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
                            )
                        }
                    );
                    dateParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" });
                    body.Append(dateParagraph);

                    Table table = new Table();

                    TableProperties tableProperties = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    );
                    table.AppendChild(tableProperties);

                    // Заголовки
                    TableRow headerRow = new TableRow();
                    AddTableCell(headerRow, "№");
                    AddTableCell(headerRow, "Фамилия");
                    AddTableCell(headerRow, "Имя");
                    AddTableCell(headerRow, "Название статьи");
                    AddTableCell(headerRow, "СМИ");
                    AddTableCell(headerRow, "Реклама");
                    AddTableCell(headerRow, "Сумма");
                    AddTableCell(headerRow, "Повышение по лучшему материалу, %");
                    AddTableCell(headerRow, "Итог");
                    AddTableCell(headerRow, "Тип издания");
                    AddTableCell(headerRow, "Дата публикации");

                    // Формат заголовков
                    foreach (var cell in headerRow.Elements<TableCell>())
                    {
                        cell.TableCellProperties = new TableCellProperties(
                            new Shading
                            {
                                Fill = "D9D9D9",
                                Val = ShadingPatternValues.Clear
                            },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                            new TableCellMargin(
                                new TopMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                                new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                                new LeftMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                                new RightMargin { Width = "20", Type = TableWidthUnitValues.Dxa }
                            )
                        );


                    }

                    table.Append(headerRow);

                    // Начинка
                    for (int i = 0; i < articles.Count; i++)
                    {
                        var article = articles[i];
                        TableRow dataRow = new TableRow();
                        AddTableCell(dataRow, (i + 1).ToString());
                        AddTableCell(dataRow, article.Employee?.SurName ?? string.Empty);
                        AddTableCell(dataRow, article.Employee?.Name ?? string.Empty);
                        AddTableCell(dataRow, article.ArticleName);
                        AddTableCell(dataRow, article.SMI);
                        AddTableCell(dataRow, article.Reklama ? "Да" : "Нет");
                        AddTableCell(dataRow, article.Summa.ToString("N2"));
                        AddTableCell(dataRow, article.Bonus.HasValue ? article.Bonus.Value.ToString("N0") + "%" : string.Empty);
                        AddTableCell(dataRow, article.Itog.ToString("N2"));
                        AddTableCell(dataRow, article.ContentType);
                        AddTableCell(dataRow, article.ReleaseDate.ToString("dd.MM.yyyy"));

                        table.Append(dataRow);
                    }

                    body.Append(table);
                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                // Подтверждение открытия файла через DialogService
                var openFile = await DialogService.ShowConfirmationDialog(
                    "Отчет успешно сохранен",
                    $"Отчет сохранен по пути:\n{filePath}\n\nХотите открыть документ сейчас?");

                if (openFile)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка при генерации отчета:\n{ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        private void AddTableCell(TableRow row, string text)
        {
            Paragraph paragraph = new Paragraph(
                new Run(new Text(text))
                {
                    RunProperties = new RunProperties(
                        new Bold(),
                        new FontSize { Val = "16" },
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
                    )
                }
            );

            paragraph.ParagraphProperties = new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { Before = "0", After = "0" }
            );

            TableCell cell = new TableCell(paragraph);

            cell.TableCellProperties = new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Auto },
                new NoWrap(),
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                new TableCellMargin(
                    new TopMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                    new LeftMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                    new RightMargin { Width = "20", Type = TableWidthUnitValues.Dxa }
                )
            );

            row.Append(cell);
        }
    }
}
