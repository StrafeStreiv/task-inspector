const API_BASE = '/api';

// Загрузка списка исполнителей из конфига (можно получить отдельным эндпоинтом, но здесь захардкодим для простоты)
const ASSIGNEES = ['Иван', 'Мария', 'Петр']; 

let currentTaskId = null;

document.addEventListener('DOMContentLoaded', () => {
    // Заполняем выпадающие списки исполнителей
    populateAssigneeSelects();
    // Загружаем задачи при активации вкладки
    document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(tab => {
        tab.addEventListener('shown.bs.tab', onTabShown);
    });
    // Первоначальная загрузка
    loadTasks();
    loadAnalytics();
    // Обработчики фильтров
    document.getElementById('statusFilter').addEventListener('change', loadTasks);
    document.getElementById('assigneeFilter').addEventListener('change', loadTasks);
    // Форма создания
    document.getElementById('createForm').addEventListener('submit', onCreateTask);
});

function populateAssigneeSelects() {
    const selects = ['#assigneeFilter', '#assignee'];
    selects.forEach(selector => {
        const select = document.querySelector(selector);
        if (!select) return;
        select.innerHTML = '<option value="">Все исполнители</option>';
        ASSIGNEES.forEach(a => {
            const option = document.createElement('option');
            option.value = a;
            option.textContent = a;
            select.appendChild(option);
        });
    });
}

function onTabShown(e) {
    const targetId = e.target.getAttribute('href');
    if (targetId === '#list') loadTasks();
    if (targetId === '#analytics') loadAnalytics();
}

// Загрузка задач
async function loadTasks() {
    const statusFilter = document.getElementById('statusFilter').value;
    const assigneeFilter = document.getElementById('assigneeFilter').value;

    let url = `${API_BASE}/tasks`;
    const params = new URLSearchParams();
    if (statusFilter && statusFilter !== 'Overdue') params.append('status', statusFilter);
    if (assigneeFilter) params.append('assignee', assigneeFilter);
    if (params.toString()) url += '?' + params.toString();

    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error('Ошибка загрузки');
        let tasks = await response.json();

        // Если выбран фильтр "Просроченные", фильтруем на клиенте
        if (statusFilter === 'Overdue') {
            tasks = tasks.filter(t => t.isOverdue);
        }

        renderTasks(tasks);
    } catch (err) {
        alert('Не удалось загрузить задачи: ' + err.message);
    }
}

function renderTasks(tasks) {
    const container = document.getElementById('tasksList');
    container.innerHTML = '';
    if (tasks.length === 0) {
        container.innerHTML = '<div class="alert alert-info">Нет задач</div>';
        return;
    }

    tasks.forEach(task => {
        const item = document.createElement('a');
        item.href = '#';
        item.className = `list-group-item list-group-item-action ${task.isOverdue ? 'overdue' : ''}`;
        item.innerHTML = `
            <div class="d-flex w-100 justify-content-between">
                <h6 class="mb-1">${task.title}</h6>
                <small>${task.priority}</small>
            </div>
            <p class="mb-1">Исполнитель: ${task.assignee} | Статус: ${task.status}</p>
            <small>Срок: ${task.deadline ? new Date(task.deadline).toLocaleString() : 'не указан'}</small>
        `;
        item.addEventListener('click', (e) => {
            e.preventDefault();
            openTaskModal(task.id);
        });
        container.appendChild(item);
    });
}

// Открытие модалки с карточкой задачи
async function openTaskModal(taskId) {
    currentTaskId = taskId;
    try {
        const response = await fetch(`${API_BASE}/tasks/${taskId}`);
        if (!response.ok) throw new Error('Ошибка загрузки');
        const task = await response.json();

        const modalBody = document.getElementById('taskModalBody');
        modalBody.innerHTML = `
            <dl>
                <dt>Название</dt><dd>${task.title}</dd>
                <dt>Описание</dt><dd>${task.description || '—'}</dd>
                <dt>Приоритет</dt><dd>${task.priority}</dd>
                <dt>Статус</dt><dd>${task.status}</dd>
                <dt>Срок</dt><dd>${task.deadline ? new Date(task.deadline).toLocaleString() : 'не указан'}</dd>
                <dt>Исполнитель</dt><dd>${task.assignee}</dd>
                <dt>Создана</dt><dd>${new Date(task.createdAt).toLocaleString()}</dd>
                <dt>Завершена</dt><dd>${task.completedAt ? new Date(task.completedAt).toLocaleString() : '—'}</dd>
                <dt>Просрочена</dt><dd>${task.isOverdue ? 'Да' : 'Нет'}</dd>
                <dt>Комментарий</dt><dd>${task.comment || '—'}</dd>
            </dl>
            <hr>
            <div class="d-flex gap-2">
                ${task.status === 'Open' ? '<button class="btn btn-sm btn-primary" onclick="changeStatus(\'InProgress\')">Взять в работу</button>' : ''}
                ${task.status === 'InProgress' ? '<button class="btn btn-sm btn-success" onclick="changeStatus(\'Completed\')">Завершить</button>' : ''}
            </div>
            <hr>
            <h6>Добавить комментарий</h6>
            <div class="input-group">
                <input type="text" id="commentInput" class="form-control" placeholder="Комментарий">
                <button class="btn btn-outline-secondary" onclick="addComment()">Отправить</button>
            </div>
        `;

        const modal = new bootstrap.Modal(document.getElementById('taskModal'));
        modal.show();
    } catch (err) {
        alert('Ошибка загрузки задачи: ' + err.message);
    }
}

// Изменение статуса
async function changeStatus(newStatus) {
    if (!currentTaskId) return;
    try {
        const response = await fetch(`${API_BASE}/tasks/${currentTaskId}/status`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ status: newStatus })
        });
        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.error || 'Ошибка');
        }
        // Обновить модалку и список
        openTaskModal(currentTaskId);
        loadTasks();
    } catch (err) {
        alert('Не удалось изменить статус: ' + err.message);
    }
}

// Добавление комментария
async function addComment() {
    const comment = document.getElementById('commentInput').value;
    if (!comment) return;
    try {
        const response = await fetch(`${API_BASE}/tasks/${currentTaskId}/comments`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ comment })
        });
        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.error || 'Ошибка');
        }
        document.getElementById('commentInput').value = '';
        openTaskModal(currentTaskId); // обновить
        loadTasks();
    } catch (err) {
        alert('Ошибка добавления комментария: ' + err.message);
    }
}

// Создание задачи
async function onCreateTask(e) {
    e.preventDefault();
    const title = document.getElementById('title').value;
    const description = document.getElementById('description').value;
    const priority = document.getElementById('priority').value;
    const deadlineInput = document.getElementById('deadline').value;
    const assignee = document.getElementById('assignee').value;

    const deadline = deadlineInput ? new Date(deadlineInput).toISOString() : null;

    const data = { title, description, priority, deadline, assignee };

    try {
        const response = await fetch(`${API_BASE}/tasks`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.error || 'Ошибка');
        }
        // Переключиться на список
        document.querySelector('a[href="#list"]').click();
        loadTasks();
        // Очистить форму
        document.getElementById('createForm').reset();
    } catch (err) {
        alert('Ошибка создания задачи: ' + err.message);
    }
}

// Загрузка аналитики
async function loadAnalytics() {
    try {
        const response = await fetch(`${API_BASE}/analytics`);
        if (!response.ok) throw new Error('Ошибка загрузки');
        const analytics = await response.json();
        renderAnalytics(analytics);
    } catch (err) {
        alert('Ошибка загрузки аналитики: ' + err.message);
    }
}

function renderAnalytics(a) {
    const container = document.getElementById('analyticsContent');
    container.innerHTML = `
        <ul class="list-group">
            <li class="list-group-item">Всего задач: ${a.totalTasks}</li>
            <li class="list-group-item">Открыто: ${a.openCount}</li>
            <li class="list-group-item">В работе: ${a.inProgressCount}</li>
            <li class="list-group-item">Завершено: ${a.completedCount}</li>
            <li class="list-group-item">Просрочено: ${a.overdueCount}</li>
            <li class="list-group-item">Среднее время выполнения (часы): ${a.averageCompletionTimeHours?.toFixed(1) ?? '—'}</li>
            <li class="list-group-item">Исполнитель с наибольшим числом просроченных: ${a.topOverdueAssignee ?? '—'}</li>
        </ul>
    `;
}