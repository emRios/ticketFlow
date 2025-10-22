// Script Node.js para generar token JWT válido
const crypto = require('crypto');

// Configuración del backend
const JWT_CONFIG = {
    secretKey: 'SuperSecretKey123456789012345678901234567890',
    issuer: 'TicketFlowApi',
    audience: 'TicketFlowClient'
};

// Datos del usuario
const USER_DATA = {
    userId: 'u123',
    username: 'testuser',
    role: 'AGENT',
    email: 'agent@test.com',
    expirationHours: 24
};

// Función base64url encoding
function base64urlEncode(str) {
    return Buffer.from(str)
        .toString('base64')
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}

// Generar token
function generateToken() {
    const now = Math.floor(Date.now() / 1000);
    const exp = now + (USER_DATA.expirationHours * 3600);

    // Header JWT
    const header = {
        alg: 'HS256',
        typ: 'JWT'
    };

    // Payload JWT con claims correctos para .NET
    const payload = {
        sub: USER_DATA.userId,
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': USER_DATA.username,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': USER_DATA.role,
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': USER_DATA.email,
        exp: exp,
        iat: now,
        iss: JWT_CONFIG.issuer,
        aud: JWT_CONFIG.audience
    };

    // Codificar header y payload
    const encodedHeader = base64urlEncode(JSON.stringify(header));
    const encodedPayload = base64urlEncode(JSON.stringify(payload));

    // Crear mensaje a firmar
    const message = `${encodedHeader}.${encodedPayload}`;

    // Generar firma HMAC SHA256
    const signature = crypto
        .createHmac('sha256', JWT_CONFIG.secretKey)
        .update(message)
        .digest('base64')
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');

    // Token JWT completo
    const token = `${message}.${signature}`;

    return {
        token,
        expiresAt: new Date(exp * 1000).toLocaleString('es-ES'),
        payload
    };
}

// Generar y mostrar token
const result = generateToken();

console.log('\n🔐 TOKEN JWT GENERADO\n');
console.log('═'.repeat(80));
console.log('\n✅ Token:');
console.log(result.token);
console.log('\n📅 Expira el:', result.expiresAt);
console.log('\n👤 Usuario:', USER_DATA.username);
console.log('🎭 Rol:', USER_DATA.role);
console.log('📧 Email:', USER_DATA.email);
console.log('\n' + '═'.repeat(80));
console.log('\n📋 COMANDO PARA CONFIGURAR EN EL NAVEGADOR:\n');
console.log(`localStorage.setItem('jwt_token', '${result.token}'); location.reload();`);
console.log('\n' + '═'.repeat(80));
console.log('\n🚀 INSTRUCCIONES:');
console.log('1. Abre tu aplicación: http://localhost:5173');
console.log('2. Abre la consola del navegador (F12)');
console.log('3. Pega el comando de arriba');
console.log('4. Presiona Enter');
console.log('5. La página se recargará con el token configurado\n');
