// Script para crear tickets de prueba en el backend
const API_BASE_URL = 'http://localhost:5076';
const TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1MTIzIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6InRlc3R1c2VyIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQUdFTlQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZ2VudEB0ZXN0LmNvbSIsImV4cCI6MTc2MTIzNDQxOCwiaWF0IjoxNzYxMTQ4MDE4LCJpc3MiOiJUaWNrZXRGbG93QXBpIiwiYXVkIjoiVGlja2V0Rmxvd0NsaWVudCJ9.A9yFlzgXP6_efAcLmHUe4c5bHBiS15YCvkC85pfc4ac';

const SAMPLE_TICKETS = [
    {
        title: 'Configurar base de datos',
        description: 'Instalar PostgreSQL y crear esquema inicial',
        priority: 'HIGH',
        status: 'OPEN',
        assignedTo: 'u123'
    },
    {
        title: 'Diseñar mockups UI',
        description: 'Crear wireframes en Figma para las pantallas principales',
        priority: 'MEDIUM',
        status: 'OPEN',
        assignedTo: 'u123'
    },
    {
        title: 'Implementar autenticación JWT',
        description: 'Configurar middleware de autenticación con tokens JWT',
        priority: 'HIGH',
        status: 'IN_PROGRESS',
        assignedTo: 'u123'
    },
    {
        title: 'Crear endpoints CRUD',
        description: 'Desarrollar API REST para gestión de tickets',
        priority: 'MEDIUM',
        status: 'IN_PROGRESS',
        assignedTo: 'u123'
    },
    {
        title: 'Documentar API',
        description: 'Generar documentación con Swagger/OpenAPI',
        priority: 'LOW',
        status: 'BLOCKED',
        assignedTo: 'u123'
    },
    {
        title: 'Implementar drag & drop',
        description: 'Añadir funcionalidad de arrastrar y soltar en el tablero Kanban',
        priority: 'MEDIUM',
        status: 'REVIEW',
        assignedTo: 'u123'
    },
    {
        title: 'Deploy a producción',
        description: 'Configurar pipeline CI/CD y desplegar en Azure',
        priority: 'HIGH',
        status: 'DONE',
        assignedTo: 'u123'
    }
];

async function createTicket(ticket) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/tickets`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${TOKEN}`
            },
            body: JSON.stringify(ticket)
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`HTTP ${response.status}: ${error}`);
        }

        const data = await response.json();
        console.log(`✅ Ticket creado: "${ticket.title}" (${ticket.status})`);
        return data;
    } catch (error) {
        console.error(`❌ Error creando ticket "${ticket.title}":`, error.message);
        throw error;
    }
}

async function seedData() {
    console.log('\n🌱 Insertando datos de prueba...\n');
    console.log('═'.repeat(60));

    let successCount = 0;
    let errorCount = 0;

    for (const ticket of SAMPLE_TICKETS) {
        try {
            await createTicket(ticket);
            successCount++;
        } catch (error) {
            errorCount++;
        }
    }

    console.log('═'.repeat(60));
    console.log(`\n📊 Resultados:`);
    console.log(`   ✅ Exitosos: ${successCount}`);
    console.log(`   ❌ Errores: ${errorCount}`);
    console.log(`\n🎉 ¡Datos insertados! Recarga tu frontend para verlos.\n`);
}

// Ejecutar
seedData().catch(error => {
    console.error('\n❌ Error fatal:', error);
    process.exit(1);
});
