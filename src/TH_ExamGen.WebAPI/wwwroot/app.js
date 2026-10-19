// Global State
let currentVietMatrix = [];
let currentMathMatrix = [];
let currentVietPaper = null;
let currentMathPaper = null;
let appSettings = {};

// Helper: Show/Hide Loading
function showLoading(msg = "Đang xử lý, vui lòng chờ...") {
    document.getElementById("loadingText").innerText = msg;
    document.getElementById("loadingOverlay").style.display = "flex";
}

function hideLoading() {
    document.getElementById("loadingOverlay").style.display = "none";
}

// Helper: Download Blob as File
function downloadBlob(blob, filename) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
}

// Tab Switching
function switchTab(tab) {
    document.getElementById("tabVietBtn").classList.toggle("active", tab === "viet");
    document.getElementById("tabMathBtn").classList.toggle("active", tab === "math");
    document.getElementById("tabViet").style.display = tab === "viet" ? "block" : "none";
    document.getElementById("tabMath").style.display = tab === "math" ? "block" : "none";
}

// ================= TIẾNG VIỆT =================

async function loadVietCurriculum() {
    const grade = document.getElementById("vietGradeSelect").value;
    const period = document.getElementById("vietPeriodSelect").value;

    showLoading("Đang tải ma trận chuẩn Tiếng Việt...");
    try {
        const res = await fetch(`/api/curriculum/viet/matrix?grade=${grade}&period=${encodeURIComponent(period)}`);
        if (!res.ok) throw new Error("Không thể tải ma trận từ máy chủ.");
        const data = await res.json();
        currentVietMatrix = data.items;

        // Render Themes
        const themeSelect = document.getElementById("vietThemeSelect");
        themeSelect.innerHTML = "";
        data.confirmedTopics.forEach(t => {
            const opt = document.createElement("option");
            opt.value = t;
            opt.innerText = t;
            themeSelect.appendChild(opt);
        });

        if (data.defaultTheme) {
            document.getElementById("vietThemeCustomInput").value = data.defaultTheme;
        }

        renderVietMatrixTable();
        updateVietSummary();
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

function renderVietMatrixTable() {
    const tbody = document.getElementById("vietMatrixBody");
    tbody.innerHTML = "";

    currentVietMatrix.forEach((item, idx) => {
        const tr = document.createElement("tr");

        tr.innerHTML = `
            <td style="text-align: center; font-weight: 600;">${item.soThuTu}</td>
            <td contenteditable="true" onblur="updateVietItem(${idx}, 'machKienThuc', this.innerText)">${item.machKienThuc}</td>
            <td contenteditable="true" onblur="updateVietItem(${idx}, 'noiDungKienThuc', this.innerText)">${item.noiDungKienThuc}</td>
            <td contenteditable="true" onblur="updateVietItem(${idx}, 'yeuCauCanDat', this.innerText)">${item.yeuCauCanDat}</td>
            <td style="text-align: center;">
                <select onchange="updateVietItem(${idx}, 'mucDo', this.value); updateVietSummary();" style="padding: 2px 4px; font-size: 0.75rem;">
                    <option value="M1" ${item.mucDo === 'M1' ? 'selected' : ''}>M1</option>
                    <option value="M2" ${item.mucDo === 'M2' ? 'selected' : ''}>M2</option>
                    <option value="M3" ${item.mucDo === 'M3' ? 'selected' : ''}>M3</option>
                </select>
            </td>
            <td style="text-align: center;">
                <select onchange="updateVietItem(${idx}, 'hinhThuc', this.value); updateVietSummary();" style="padding: 2px 4px; font-size: 0.75rem;">
                    <option value="TN" ${item.hinhThuc === 'TN' ? 'selected' : ''}>TN</option>
                    <option value="TL" ${item.hinhThuc === 'TL' ? 'selected' : ''}>TL</option>
                </select>
            </td>
            <td style="text-align: center;">
                <input type="number" step="0.5" min="0" max="10" value="${item.diem}" 
                       onchange="updateVietItem(${idx}, 'diem', parseFloat(this.value) || 0); updateVietSummary();" 
                       style="width: 55px; text-align: center; padding: 2px 4px; font-size: 0.8rem;">
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function updateVietItem(index, prop, value) {
    if (currentVietMatrix[index]) {
        currentVietMatrix[index][prop] = value;
    }
}

function updateVietSummary() {
    let total = 0, reading = 0, writing = 0;
    let m1 = 0, m2 = 0, m3 = 0;
    let tn = 0, tl = 0;

    currentVietMatrix.forEach(i => {
        const d = parseFloat(i.diem) || 0;
        total += d;

        if (i.machKienThuc?.includes("Đọc hiểu")) reading += d;
        else writing += d;

        if (i.mucDo === "M1") m1 += d;
        else if (i.mucDo === "M2") m2 += d;
        else if (i.mucDo === "M3") m3 += d;

        if (i.hinhThuc === "TN") tn += d;
        else tl += d;
    });

    document.getElementById("vTotalPoints").innerText = `${total.toFixed(1)}đ`;
    document.getElementById("vReadingPoints").innerText = `${reading.toFixed(1)}đ`;
    document.getElementById("vWritingPoints").innerText = `${writing.toFixed(1)}đ`;
    document.getElementById("vLevelsRatio").innerText = `${m1.toFixed(1)} / ${m2.toFixed(1)} / ${m3.toFixed(1)}`;
    document.getElementById("vTypeRatio").innerText = `${tn.toFixed(1)}đ / ${tl.toFixed(1)}đ`;
}

function toggleVietPassageMode() {
    const isAi = document.querySelector('input[name="vietPassageMode"]:checked').value === 'ai';
    document.getElementById("vietAiThemePnl").style.display = isAi ? "block" : "none";
    document.getElementById("vietCustomPassagePnl").style.display = isAi ? "none" : "block";
}

function onVietThemeSelected() {
    const sel = document.getElementById("vietThemeSelect");
    document.getElementById("vietThemeCustomInput").value = sel.value;
}

async function auditVietMatrix() {
    const grade = parseInt(document.getElementById("vietGradeSelect").value);
    const period = document.getElementById("vietPeriodSelect").value;

    showLoading("AI đang rà soát ma trận Tiếng Việt...");
    try {
        const res = await fetch("/api/ai/viet/audit-matrix", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ grade, period, matrix: currentVietMatrix })
        });
        const data = await res.json();
        if (data.report) {
            alert("KẾT QUẢ RÀ SOÁT MA TRẬN:\n\n" + data.report);
        } else {
            alert("Lỗi: " + (data.error || "Không nhận được phản hồi từ AI."));
        }
    } catch (err) {
        alert("Lỗi kết nối: " + err.message);
    } finally {
        hideLoading();
    }
}

async function exportVietMatrix() {
    const grade = parseInt(document.getElementById("vietGradeSelect").value);
    const period = document.getElementById("vietPeriodSelect").value;

    showLoading("Đang kết xuất tệp Word Ma trận & Bản đặc tả...");
    try {
        const res = await fetch("/api/export/viet/matrix", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ grade, period, matrix: currentVietMatrix })
        });
        if (!res.ok) throw new Error("Xuất file thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `MaTran_DacTa_TiengViet_Lop${grade}_${period.replace(/\s+/g, '_')}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

async function generateVietExam() {
    const grade = parseInt(document.getElementById("vietGradeSelect").value);
    const period = document.getElementById("vietPeriodSelect").value;
    const isAiPassage = document.querySelector('input[name="vietPassageMode"]:checked').value === 'ai';
    const theme = document.getElementById("vietThemeCustomInput").value.trim() || document.getElementById("vietThemeSelect").value;

    let passage = null;
    if (!isAiPassage) {
        const title = document.getElementById("customPassageTitle").value.trim();
        const content = document.getElementById("customPassageContent").value.trim();
        if (!content) {
            alert("Vui lòng nhập nội dung bài đọc!");
            return;
        }
        passage = {
            tieuDe: title || "BÀI ĐỌC HIỂU",
            tacGia: document.getElementById("customPassageAuthor").value.trim() || "Sưu tầm",
            nguonGoc: "Ngoài-SGK",
            nguonChiTiet: document.getElementById("customPassageSource").value.trim() || "Tự soạn",
            noiDung: content,
            chuDe: theme
        };
    }

    showLoading("AI đang sinh bài đọc và câu hỏi đề thi Tiếng Việt... Quá trình có thể mất 30-45 giây.");
    try {
        const res = await fetch("/api/ai/viet/generate-exam", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                grade,
                period,
                durationMinutes: 35,
                theme,
                matrix: currentVietMatrix,
                passage
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.error || "Không thể sinh đề thi");
        }

        currentVietPaper = await res.json();
        renderVietExamPreview(currentVietPaper);

        document.getElementById("btnExportVietExam").disabled = false;
        document.getElementById("btnExportVietFull").disabled = false;
    } catch (err) {
        alert("Lỗi sinh đề: " + err.message);
    } finally {
        hideLoading();
    }
}

function renderVietExamPreview(paper) {
    const container = document.getElementById("vietPreviewArea");
    container.innerHTML = "";

    // Header info
    const headerHtml = `
        <div style="background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 12px; margin-bottom: 1rem;">
            <h3 style="color: var(--primary); font-size: 1.1rem; text-align: center; margin-bottom: 4px;">${paper.tieuDe}</h3>
            <div style="text-align: center; font-size: 0.85rem; color: var(--gray-600);">
                Môn: <strong>${paper.monHoc}</strong> | Lớp: <strong>${paper.khoiLop}</strong> | Thời gian: <strong>${paper.thoiGianPhut} phút</strong>
            </div>
        </div>
    `;
    container.innerHTML += headerHtml;

    // Reading passage
    if (paper.baiDoc && paper.baiDoc.noiDung) {
        const passageHtml = `
            <div class="passage-preview">
                <div class="passage-title">${paper.baiDoc.tieuDe}</div>
                <div class="passage-author">${paper.baiDoc.tacGia ? 'Tác giả: ' + paper.baiDoc.tacGia : ''}</div>
                <div style="white-space: pre-line; line-height: 1.6;">${paper.baiDoc.noiDung}</div>
            </div>
        `;
        container.innerHTML += passageHtml;
    }

    // Questions
    if (paper.danhSachCauHoi && paper.danhSachCauHoi.length > 0) {
        paper.danhSachCauHoi.forEach(q => {
            let optionsHtml = "";
            if (q.cacLuaChon && q.cacLuaChon.length > 0) {
                optionsHtml = '<div class="options-grid">';
                q.cacLuaChon.forEach((opt, oIdx) => {
                    const char = String.fromCharCode(65 + oIdx);
                    const isCorrect = q.dapAnDung?.trim().toUpperCase() === char;
                    optionsHtml += `<div class="option-cell ${isCorrect ? 'correct' : ''}"><strong>${char}.</strong> ${opt}</div>`;
                });
                optionsHtml += '</div>';
            }

            const qHtml = `
                <div class="question-item">
                    <div class="question-header">
                        <span>Câu ${q.soThuTu} (${q.diem} điểm) [${q.mucDo} - ${q.hinhThuc}]</span>
                        <span style="font-size: 0.8rem; color: var(--gray-600);">${q.machKienThuc || ''}</span>
                    </div>
                    <div class="question-body">${q.noiDung}</div>
                    ${optionsHtml}
                    ${q.huongDanCham ? `<div class="grading-guide"><strong>💡 Hướng dẫn chấm / Đáp án:</strong> ${q.huongDanCham}</div>` : ''}
                </div>
            `;
            container.innerHTML += qHtml;
        });
    }
}

async function exportVietExamOnly() {
    if (!currentVietPaper) return;
    showLoading("Đang kết xuất đề thi Word...");
    try {
        const res = await fetch("/api/export/viet/exam", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ paper: currentVietPaper, skipValidation: false })
        });
        if (!res.ok) throw new Error("Xuất đề thi thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `DeThi_TiengViet_Lop${currentVietPaper.khoiLop}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

async function exportVietFullPackage() {
    if (!currentVietPaper) return;
    showLoading("Đang kết xuất trọn bộ Đề thi và Hướng dẫn chấm...");
    try {
        const res = await fetch("/api/export/viet/full", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ paper: currentVietPaper, skipValidation: false })
        });
        if (!res.ok) throw new Error("Xuất trọn bộ thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `TronBo_DeThi_Va_DapAn_TiengViet_Lop${currentVietPaper.khoiLop}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

// ================= MÔN TOÁN =================

async function loadMathCurriculum() {
    const grade = document.getElementById("mathGradeSelect").value;
    const period = document.getElementById("mathPeriodSelect").value;

    showLoading("Đang tải ma trận chuẩn môn Toán...");
    try {
        const res = await fetch(`/api/curriculum/math/matrix?grade=${grade}&period=${encodeURIComponent(period)}`);
        if (!res.ok) throw new Error("Không thể tải ma trận từ máy chủ.");
        const data = await res.json();
        currentMathMatrix = data.items;

        renderMathMatrixTable();
        updateMathSummary();
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

function renderMathMatrixTable() {
    const tbody = document.getElementById("mathMatrixBody");
    tbody.innerHTML = "";

    currentMathMatrix.forEach((item, idx) => {
        const tr = document.createElement("tr");

        tr.innerHTML = `
            <td style="text-align: center; font-weight: 600;">${item.soThuTu}</td>
            <td contenteditable="true" onblur="updateMathItem(${idx}, 'chuDe', this.innerText)">${item.chuDe}</td>
            <td contenteditable="true" onblur="updateMathItem(${idx}, 'noiDungKienThuc', this.innerText)">${item.noiDungKienThuc}</td>
            <td contenteditable="true" onblur="updateMathItem(${idx}, 'yeuCauCanDat', this.innerText)">${item.yeuCauCanDat}</td>
            <td style="text-align: center;">
                <select onchange="updateMathItem(${idx}, 'mucDo', this.value); updateMathSummary();" style="padding: 2px 4px; font-size: 0.75rem;">
                    <option value="M1" ${item.mucDo === 'M1' ? 'selected' : ''}>M1</option>
                    <option value="M2" ${item.mucDo === 'M2' ? 'selected' : ''}>M2</option>
                    <option value="M3" ${item.mucDo === 'M3' ? 'selected' : ''}>M3</option>
                </select>
            </td>
            <td style="text-align: center;">
                <select onchange="updateMathItem(${idx}, 'hinhThuc', this.value); updateMathSummary();" style="padding: 2px 4px; font-size: 0.75rem;">
                    <option value="TN" ${item.hinhThuc === 'TN' ? 'selected' : ''}>TN</option>
                    <option value="TL" ${item.hinhThuc === 'TL' ? 'selected' : ''}>TL</option>
                </select>
            </td>
            <td style="text-align: center;">
                <input type="number" step="0.5" min="0" max="10" value="${item.diem}" 
                       onchange="updateMathItem(${idx}, 'diem', parseFloat(this.value) || 0); updateMathSummary();" 
                       style="width: 55px; text-align: center; padding: 2px 4px; font-size: 0.8rem;">
            </td>
            <td style="text-align: center;">
                <input type="checkbox" ${item.coHinhTikz ? 'checked' : ''} onchange="updateMathItem(${idx}, 'coHinhTikz', this.checked)">
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function updateMathItem(index, prop, value) {
    if (currentMathMatrix[index]) {
        currentMathMatrix[index][prop] = value;
    }
}

function updateMathSummary() {
    let total = 0, m1 = 0, m2 = 0, m3 = 0, tn = 0, tl = 0;

    currentMathMatrix.forEach(i => {
        const d = parseFloat(i.diem) || 0;
        total += d;
        if (i.mucDo === "M1") m1 += d;
        else if (i.mucDo === "M2") m2 += d;
        else if (i.mucDo === "M3") m3 += d;

        if (i.hinhThuc === "TN") tn += d;
        else tl += d;
    });

    document.getElementById("mTotalPoints").innerText = `${total.toFixed(1)}đ`;
    document.getElementById("mLevelsRatio").innerText = `${m1.toFixed(1)} / ${m2.toFixed(1)} / ${m3.toFixed(1)}`;
    document.getElementById("mTypeRatio").innerText = `${tn.toFixed(1)}đ / ${tl.toFixed(1)}đ`;
}

async function auditMathMatrix() {
    const grade = parseInt(document.getElementById("mathGradeSelect").value);
    const period = document.getElementById("mathPeriodSelect").value;

    showLoading("AI đang rà soát ma trận môn Toán...");
    try {
        const res = await fetch("/api/ai/math/audit-matrix", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ grade, period, matrix: currentMathMatrix })
        });
        const data = await res.json();
        if (data.report) {
            alert("KẾT QUẢ RÀ SOÁT MA TRẬN TOÁN:\n\n" + data.report);
        } else {
            alert("Lỗi: " + (data.error || "Không nhận được phản hồi từ AI."));
        }
    } catch (err) {
        alert("Lỗi kết nối: " + err.message);
    } finally {
        hideLoading();
    }
}

async function exportMathMatrix() {
    const grade = parseInt(document.getElementById("mathGradeSelect").value);
    const period = document.getElementById("mathPeriodSelect").value;

    showLoading("Đang kết xuất tệp Word Ma trận & Bản đặc tả môn Toán...");
    try {
        const res = await fetch("/api/export/math/matrix", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ grade, period, matrix: currentMathMatrix })
        });
        if (!res.ok) throw new Error("Xuất file thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `MaTran_DacTa_Toan_Lop${grade}_${period.replace(/\s+/g, '_')}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

async function generateMathExam() {
    const grade = parseInt(document.getElementById("mathGradeSelect").value);
    const period = document.getElementById("mathPeriodSelect").value;

    showLoading("AI đang sinh các câu hỏi và đáp án môn Toán... Quá trình có thể mất 30-40 giây.");
    try {
        const res = await fetch("/api/ai/math/generate-exam", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                grade,
                period,
                durationMinutes: 40,
                matrix: currentMathMatrix
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.error || "Không thể sinh đề thi");
        }

        currentMathPaper = await res.json();
        renderMathExamPreview(currentMathPaper);

        document.getElementById("btnExportMathExam").disabled = false;
        document.getElementById("btnExportMathFull").disabled = false;
    } catch (err) {
        alert("Lỗi sinh đề: " + err.message);
    } finally {
        hideLoading();
    }
}

function renderMathExamPreview(paper) {
    const container = document.getElementById("mathPreviewArea");
    container.innerHTML = "";

    const headerHtml = `
        <div style="background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 12px; margin-bottom: 1rem;">
            <h3 style="color: var(--primary); font-size: 1.1rem; text-align: center; margin-bottom: 4px;">${paper.tieuDe}</h3>
            <div style="text-align: center; font-size: 0.85rem; color: var(--gray-600);">
                Môn: <strong>${paper.monHoc}</strong> | Lớp: <strong>${paper.khoiLop}</strong> | Thời gian: <strong>${paper.thoiGianPhut} phút</strong>
            </div>
        </div>
    `;
    container.innerHTML += headerHtml;

    if (paper.danhSachCauHoi && paper.danhSachCauHoi.length > 0) {
        paper.danhSachCauHoi.forEach(q => {
            let optionsHtml = "";
            if (q.cacLuaChon && q.cacLuaChon.length > 0) {
                optionsHtml = '<div class="options-grid">';
                q.cacLuaChon.forEach((opt, oIdx) => {
                    const char = String.fromCharCode(65 + oIdx);
                    const isCorrect = q.dapAnDung?.trim().toUpperCase() === char;
                    optionsHtml += `<div class="option-cell ${isCorrect ? 'correct' : ''}"><strong>${char}.</strong> ${opt}</div>`;
                });
                optionsHtml += '</div>';
            }

            let stepsHtml = "";
            if (q.cacBuocGiai && q.cacBuocGiai.length > 0) {
                stepsHtml = '<div style="margin-top: 6px; font-size: 0.825rem; color: #1e3a8a;"><strong>Các bước giải:</strong><ul style="margin-left: 20px;">';
                q.cacBuocGiai.forEach(s => {
                    stepsHtml += `<li>Bước ${s.buoc}: ${s.noiDung} (${s.diem}đ)</li>`;
                });
                stepsHtml += '</ul></div>';
            }

            const qHtml = `
                <div class="question-item">
                    <div class="question-header">
                        <span>Câu ${q.soThuTu} (${q.diem} điểm) [${q.mucDo} - ${q.hinhThuc}]</span>
                        <span style="font-size: 0.8rem; color: var(--gray-600);">${q.chuDe || ''}</span>
                    </div>
                    <div class="question-body">${q.noiDung}</div>
                    ${optionsHtml}
                    ${q.dapAnDung ? `<div class="grading-guide"><strong>💡 Đáp án đúng:</strong> ${q.dapAnDung}</div>` : ''}
                    ${stepsHtml}
                    ${q.huongDanCham ? `<div class="grading-guide" style="margin-top: 4px;"><strong>💡 Hướng dẫn chấm:</strong> ${q.huongDanCham}</div>` : ''}
                </div>
            `;
            container.innerHTML += qHtml;
        });
    }
}

async function exportMathExamOnly() {
    if (!currentMathPaper) return;
    showLoading("Đang kết xuất đề thi Toán Word...");
    try {
        const res = await fetch("/api/export/math/exam", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ paper: currentMathPaper, skipValidation: false })
        });
        if (!res.ok) throw new Error("Xuất đề thi thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `DeThi_Toan_Lop${currentMathPaper.khoiLop}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

async function exportMathFullPackage() {
    if (!currentMathPaper) return;
    showLoading("Đang kết xuất trọn bộ Đề thi và Hướng dẫn chấm môn Toán...");
    try {
        const res = await fetch("/api/export/math/full", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ paper: currentMathPaper, skipValidation: false })
        });
        if (!res.ok) throw new Error("Xuất trọn bộ thất bại");
        const blob = await res.blob();
        downloadBlob(blob, `TronBo_DeThi_Va_DapAn_Toan_Lop${currentMathPaper.khoiLop}.docx`);
    } catch (err) {
        alert("Lỗi: " + err.message);
    } finally {
        hideLoading();
    }
}

// ================= SETTINGS MODAL =================

function openSettingsModal() {
    loadSettings();
    document.getElementById("settingsModal").style.display = "flex";
}

function closeSettingsModal() {
    document.getElementById("settingsModal").style.display = "none";
}

async function loadSettings() {
    try {
        const res = await fetch("/api/settings");
        if (res.ok) {
            const data = await res.json();
            appSettings = data;
            document.getElementById("cfgUbnd").value = data.tenUBND || "";
            document.getElementById("cfgTruong").value = data.tenTruong || "";
            document.getElementById("cfgNamHoc").value = data.namHoc || "";
            document.getElementById("cfgAiProvider").value = data.aiProvider || "Gemini";
            if (data.geminiModel) {
                document.getElementById("cfgGeminiModel").value = data.geminiModel;
            }
            if (data.hasGeminiKey) {
                document.getElementById("cfgGeminiKey").placeholder = data.geminiApiKey || "(Đã lưu khóa)";
            }
        }
    } catch (e) {
        console.error("Could not load settings", e);
    }
}

async function saveSettings() {
    const payload = {
        tenUBND: document.getElementById("cfgUbnd").value.trim(),
        tenTruong: document.getElementById("cfgTruong").value.trim(),
        namHoc: document.getElementById("cfgNamHoc").value.trim(),
        aiProvider: document.getElementById("cfgAiProvider").value,
        geminiModel: document.getElementById("cfgGeminiModel").value,
        geminiApiKey: document.getElementById("cfgGeminiKey").value.trim(),
        deepSeekApiKey: document.getElementById("cfgDeepSeekKey").value.trim()
    };

    try {
        const res = await fetch("/api/settings", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            alert("Đã lưu cấu hình thành công!");
            closeSettingsModal();
        } else {
            alert("Lỗi lưu cấu hình");
        }
    } catch (e) {
        alert("Lỗi kết nối: " + e.message);
    }
}

// ================= SUGGEST & PPCT MODALS =================
let currentSuggestSubject = "viet";
let currentPpctData = [];

function onSuggestModeChange() {
    const mode = document.querySelector('input[name="suggestMode"]:checked')?.value || "mode1";
    document.getElementById("suggestContainerMode1").style.display = mode === "mode1" ? "block" : "none";
    document.getElementById("suggestContainerMode2").style.display = mode === "mode2" ? "block" : "none";
    document.getElementById("suggestContainerMode3").style.display = mode === "mode3" ? "block" : "none";

    if (mode === "mode3") {
        document.getElementById("suggestCustomTextarea").focus();
    }
}

function updateSuggestTopicCounter() {
    const cbs = document.querySelectorAll('.suggest-topic-cb');
    const checked = document.querySelectorAll('.suggest-topic-cb:checked');
    const counter = document.getElementById("suggestTopicCounter");
    if (counter) {
        counter.innerText = `Đã chọn: ${checked.length}/${cbs.length} bài học`;
    }
}

function selectAllSuggestTopics() {
    document.querySelectorAll('.suggest-topic-cb').forEach(cb => cb.checked = true);
    updateSuggestTopicCounter();
}

function deselectAllSuggestTopics() {
    document.querySelectorAll('.suggest-topic-cb').forEach(cb => cb.checked = false);
    updateSuggestTopicCounter();
}

function defaultSuggestTopics() {
    document.querySelectorAll('.suggest-topic-cb').forEach(cb => {
        cb.checked = cb.getAttribute("data-default") === "true";
    });
    updateSuggestTopicCounter();
}

async function openSuggestModal(subject) {
    currentSuggestSubject = subject;
    const grade = document.getElementById(`${subject}GradeSelect`).value;
    const period = document.getElementById(`${subject}PeriodSelect`).value;
    const subName = subject === "viet" ? "Môn Tiếng Việt" : "Môn Toán";

    // Mở modal ngay lập tức để người dùng có phản hồi thị giác
    document.getElementById("suggestModal").style.display = "flex";
    document.getElementById("suggestModalTitle").innerText = `AI Gợi ý Ma trận & Bản đặc tả - ${subName} Lớp ${grade} (${period})`;

    // Reset về Chế độ 1 mặc định
    document.getElementById("suggestMode1").checked = true;
    onSuggestModeChange();

    // Điền mô tả tự động cho Cách 2
    if (subject === "math") {
        document.getElementById("suggestAutoDesc").innerHTML = `
            <strong>Cách 2 - AI tự động phân bổ:</strong><br>
            AI sẽ bám sát phân phối chương trình đợt <em>"${period}"</em>, tự chọn các chủ đề trọng tâm cho đề Toán lớp ${grade} (thầy/cô không cần chọn thủ công), phủ đủ 3 mạch kiến thức (Số &amp; phép tính, Hình học &amp; đo lường, Thống kê &amp; xác suất) và giữ nguyên cấu trúc chuẩn (10 câu: 7 TN + 3 TL, tổng 10 điểm).
        `;
        document.getElementById("suggestCustomTextarea").value = 
            `Đề kiểm tra ${period} môn Toán lớp ${grade} sách Kết nối tri thức. Đảm bảo cấu trúc 10 câu (7 TN, 3 TL), tổng điểm 10 điểm, phân bổ M1 40%, M2 30%, M3 30%. Có câu hỏi hình vẽ trực quan.`;
    } else {
        document.getElementById("suggestAutoDesc").innerHTML = `
            <strong>Cách 2 - AI tự động phân bổ:</strong><br>
            AI sẽ bám sát phân phối chương trình đợt <em>"${period}"</em>, tự chọn chủ điểm bài đọc và các bài Luyện từ &amp; câu trọng tâm cho Tiếng Việt lớp ${grade} (thầy/cô không cần chọn thủ công), phủ đủ cấu trúc chuẩn 11 câu (7 câu đọc hiểu + 4 câu kiến thức TV, tổng 8 điểm).
        `;
        document.getElementById("suggestCustomTextarea").value = 
            `Đề kiểm tra ${period} môn Tiếng Việt lớp ${grade} sách Kết nối tri thức. Đảm bảo cấu trúc 11 câu (7 câu đọc hiểu, 4 câu kiến thức Tiếng Việt), tổng 8 điểm.`;
    }

    const counter = document.getElementById("suggestTopicCounter");
    counter.innerText = "⏳ Đang nạp danh mục PPCT...";
    const listDiv = document.getElementById("suggestTopicsList");
    listDiv.innerHTML = `<div style="text-align: center; padding: 20px; color: var(--gray-600);">Đang tải dữ liệu phân phối chương trình...</div>`;

    try {
        const res = await fetch(`/api/curriculum/${subject}/ppct?grade=${grade}&period=${encodeURIComponent(period)}`);
        if (!res.ok) throw new Error("Không thể tải danh mục PPCT");
        const data = await res.json();
        currentPpctData = data.topics || [];

        listDiv.innerHTML = "";
        if (currentPpctData.length === 0) {
            listDiv.innerHTML = `<div style="padding: 10px; color: var(--gray-600);">Chưa có danh mục chủ đề cho đợt kiểm tra này.</div>`;
            counter.innerText = "0/0";
            return;
        }

        // Nhóm theo Mạch kiến thức
        const groups = {};
        currentPpctData.forEach(t => {
            const strand = t.machKienThuc || "Chủ đề chung";
            if (!groups[strand]) groups[strand] = [];
            groups[strand].push(t);
        });

        Object.keys(groups).forEach(strand => {
            const header = document.createElement("div");
            header.style.cssText = "background: #f1f5f9; padding: 4px 8px; border-radius: 4px; margin: 8px 0 4px 0; font-weight: 700; font-size: 0.82rem; color: #1e293b;";
            header.innerText = `📌 ${strand} (${groups[strand].length} bài học)`;
            listDiv.appendChild(header);

            groups[strand].forEach(t => {
                const row = document.createElement("label");
                row.style.cssText = "display: flex; align-items: flex-start; gap: 8px; margin-bottom: 6px; font-size: 0.825rem; cursor: pointer; padding: 3px 6px; border-radius: 4px;";
                row.onmouseover = () => row.style.background = "#f8fafc";
                row.onmouseout = () => row.style.background = "transparent";

                const isDefault = t.daXacNhan !== false;
                row.innerHTML = `
                    <input type="checkbox" value="${t.maMuc}" data-default="${isDefault}" class="suggest-topic-cb" ${isDefault ? 'checked' : ''} onchange="updateSuggestTopicCounter()" style="margin-top: 3px;">
                    <div>
                        <span style="color: #0284c7; font-weight: 600;">[Tuần ${t.tuan || '?'}]</span>
                        <strong style="color: #334155;">${t.chuDe}:</strong> ${t.noiDung}
                    </div>
                `;
                listDiv.appendChild(row);
            });
        });

        updateSuggestTopicCounter();
    } catch (err) {
        listDiv.innerHTML = `<div style="color: red; padding: 10px;">Lỗi tải dữ liệu: ${err.message}</div>`;
        counter.innerText = "Lỗi kết nối";
    }
}

function closeSuggestModal() {
    document.getElementById("suggestModal").style.display = "none";
}

async function submitSuggestMatrix() {
    const subject = currentSuggestSubject;
    const grade = parseInt(document.getElementById(`${subject}GradeSelect`).value);
    const period = document.getElementById(`${subject}PeriodSelect`).value;
    const mode = document.querySelector('input[name="suggestMode"]:checked')?.value || "mode1";

    let autoSelectTopics = false;
    let selectedTopics = null;
    let requirement = "";

    if (mode === "mode1") {
        // Cách 1: Tích chọn chủ đề từ PPCT
        const cbs = document.querySelectorAll('.suggest-topic-cb:checked');
        selectedTopics = Array.from(cbs).map(cb => cb.value);
        if (selectedTopics.length === 0) {
            alert("Vui lòng tích chọn ít nhất 1 bài học/chủ điểm từ danh mục PPCT (Cách 1)!");
            return;
        }
        autoSelectTopics = false;
        requirement = "";
    } else if (mode === "mode2") {
        // Cách 2: Để AI tự chọn bám sát PPCT
        autoSelectTopics = true;
        selectedTopics = null;
        requirement = "";
    } else {
        // Cách 3: Nhập định hướng / yêu cầu riêng
        requirement = document.getElementById("suggestCustomTextarea").value.trim();
        if (!requirement) {
            alert("Vui lòng nhập định hướng kiểm tra hoặc yêu cầu riêng của thầy/cô (Cách 3)!");
            return;
        }
        autoSelectTopics = false;
        selectedTopics = null;
    }

    closeSuggestModal();
    showLoading(`AI đang phân tích PPCT và lập Ma trận & Bản đặc tả mới... Quá trình có thể mất 30-40 giây.`);

    try {
        const res = await fetch(`/api/ai/${subject}/suggest-matrix`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                grade,
                period,
                requirement,
                autoSelectTopics,
                selectedTopics
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.error || "Gợi ý ma trận thất bại");
        }

        const items = await res.json();
        if (subject === "viet") {
            currentVietMatrix = items;
            renderVietMatrixTable();
            updateVietSummary();
        } else {
            currentMathMatrix = items;
            renderMathMatrixTable();
            updateMathSummary();
        }

        alert(`Đã lập thành công Ma trận & Bản đặc tả mới theo PPCT! Bạn có thể chỉnh sửa tiếp trên bảng và bấm 'Tải Word' để xuất file.`);
    } catch (err) {
        alert("Lỗi từ AI: " + err.message);
    } finally {
        hideLoading();
    }
}

async function openPpctModal(subject) {
    const grade = document.getElementById(`${subject}GradeSelect`).value;
    const period = document.getElementById(`${subject}PeriodSelect`).value;
    const subName = subject === "viet" ? "Môn Tiếng Việt" : "Môn Toán";

    // Mở modal ngay lập tức
    document.getElementById("ppctModal").style.display = "flex";
    document.getElementById("ppctModalTitle").innerText = `Phân phối chương trình (PPCT) - ${subName} Lớp ${grade} (${period})`;

    const tbody = document.getElementById("ppctTableBody");
    tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; padding: 25px; color: var(--gray-600);">⏳ Đang tải dữ liệu phân phối chương trình...</td></tr>`;

    try {
        const res = await fetch(`/api/curriculum/${subject}/ppct?grade=${grade}&period=${encodeURIComponent(period)}`);
        if (!res.ok) throw new Error("Không thể tải danh mục PPCT");
        const data = await res.json();

        tbody.innerHTML = "";
        const list = data.topics || [];
        if (list.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; padding: 20px; color: var(--gray-600);">Chưa có dữ liệu PPCT cho đợt này.</td></tr>`;
            return;
        }

        list.forEach(t => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td style="text-align: center; font-weight: 600;">${t.tuan || ''}</td>
                <td><span class="badge badge-m1">${t.machKienThuc || ''}</span></td>
                <td><strong>${t.chuDe || ''}</strong></td>
                <td>
                    ${t.noiDung || ''}
                    ${t.yeuCauCanDatM1 ? `
                        <div style="margin-top: 6px; padding-top: 6px; border-top: 1px dashed var(--gray-200); font-size: 0.8rem; color: #475569;">
                            <div>• <strong>M1:</strong> ${t.yeuCauCanDatM1}</div>
                            <div>• <strong>M2:</strong> ${t.yeuCauCanDatM2}</div>
                            <div>• <strong>M3:</strong> ${t.yeuCauCanDatM3}</div>
                        </div>
                    ` : ''}
                </td>
            `;
            tbody.appendChild(tr);
        });
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; padding: 20px; color: red;">Lỗi tải dữ liệu: ${err.message}</td></tr>`;
    }
}

function closePpctModal() {
    document.getElementById("ppctModal").style.display = "none";
}

// Initial Load
document.addEventListener("DOMContentLoaded", () => {
    loadVietCurriculum();
    loadMathCurriculum();
});
