const { Sequelize, DataTypes } = require('sequelize');

const sequelize = new Sequelize('arkanoid_game', 'your_mysql_user', 'your_mysql_password', {
    host: 'localhost',
    dialect: 'mysql',
    logging: false
});

const User = sequelize.define('User', {
    username: { type: DataTypes.STRING, unique: true, allowNull: false },
    password_hash: { type: DataTypes.STRING, allowNull: false },
    created_at: { type: DataTypes.DATE, defaultValue: DataTypes.NOW }
}, { tableName: 'users', timestamps: false });

module.exports = { sequelize, User };