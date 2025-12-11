import React, { useContext, useEffect, useState } from "react";
import { AuthContext } from "../auth/AuthContext";
import {
  getDevices,
  updateDeviceName,
  deleteDevice,
  startRegisterExistingDevice,
  finishRegister,
  deleteAccount,
} from "../services/MultiDeviceService";

import { FaDesktop, FaSave, FaEdit, FaTrash, FaPlus, FaSignOutAlt } from "react-icons/fa";
import "./WelcomePage.css";

export default function WelcomePage() {
  const { user, token, setToken, logout } = useContext(AuthContext);

  const [devices, setDevices] = useState([]);
  const [editingId, setEditingId] = useState(null);
  const [newDeviceName, setNewDeviceName] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const [showAddModal, setShowAddModal] = useState(false);
  const [addDeviceName, setAddDeviceName] = useState("");


  // Load devices when token is ready
  useEffect(() => {
    if (!token) return;
    fetchDevices();
  }, [token]);

  const fetchDevices = async () => {
    try {
      const res = await getDevices(token);
      setDevices(res.devices || []);
    } catch {
      setMessage("Failed to fetch devices");
    }
  };


  // DELETE DEVICE (now handles token refresh)
  const handleDeleteDevice = async (credentialId) => {
    if (!window.confirm("Are you sure you want to delete this device?")) return;

    try {
      const res = await deleteDevice({ id: credentialId, token });

      // ⭐ Backend may return a new token
      if (res?.token) {
        localStorage.setItem("token", res.token);
        setToken(res.token);
      }

      setDevices((prev) => prev.filter((d) => d.credentialId !== credentialId));
    } catch {
      setMessage("Error deleting device");
    }
  };


  // ADD NEW DEVICE (now handles token refresh)
  const handleGenerateNewDevice = async () => {
    if (!addDeviceName.trim()) return;

    setLoading(true);
    setMessage("");

    try {
      const begin = await startRegisterExistingDevice(token);

      const att = await finishRegister({
        challengeId: begin.challengeId,
        attestation: begin.options,
        deviceName: addDeviceName.trim(),
        token,
      });

      // ⭐ Save new token if provided
      if (att?.token) {
        localStorage.setItem("token", att.token);
        setToken(att.token);
      }

      await fetchDevices();
      setShowAddModal(false);
      setAddDeviceName("");
    } catch {
      setMessage("Error adding device");
    } finally {
      setLoading(false);
    }
  };


  const handleSaveDeviceName = async (credentialId) => {
    if (!newDeviceName.trim()) return;
    try {
      await updateDeviceName({
        id: credentialId,
        name: newDeviceName.trim(),
        token,
      });

      setDevices((prev) =>
        prev.map((d) =>
          d.credentialId === credentialId
            ? { ...d, deviceName: newDeviceName.trim() }
            : d
        )
      );

      setEditingId(null);
      setNewDeviceName("");
    } catch {
      setMessage("Error updating device name");
    }
  };


  const handleDeleteAccount = async () => {
    if (!window.confirm("This will permanently delete your account. Continue?")) return;
    try {
      await deleteAccount(token);
      logout();
    } catch {
      setMessage("Error deleting account");
    }
  };


  // -----------------------------------------------------------------
  // UI
  // -----------------------------------------------------------------
  return (
    <div className="welcome-container modern">
      <div className="header-flex">
        <h1>Security Settings</h1>
        <button className="btn-logout" onClick={logout}>
          <FaSignOutAlt /> Logout
        </button>
      </div>

      {message && <p className="error-message">{message}</p>}

      <table className="device-table modern-table">
        <thead>
          <tr>
            <th>Device</th>
            <th>Rename</th>
            <th>Remove</th>
          </tr>
        </thead>

        <tbody>
          {devices.map((device) => (
            <tr key={device.credentialId}>
              <td className="device-cell">
                <FaDesktop style={{ marginRight: "8px" }} />
                {editingId === device.credentialId ? (
                  <input
                    value={newDeviceName}
                    onChange={(e) => setNewDeviceName(e.target.value)}
                  />
                ) : (
                  <span>{device.deviceName || "Unnamed Device"}</span>
                )}
              </td>

              <td>
                {editingId === device.credentialId ? (
                  <button
                    className="btn-small save"
                    onClick={() => handleSaveDeviceName(device.credentialId)}
                  >
                    <FaSave /> Save
                  </button>
                ) : (
                  <button
                    className="btn-small edit"
                    onClick={() => {
                      setEditingId(device.credentialId);
                      setNewDeviceName(device.deviceName || "");
                    }}
                  >
                    <FaEdit /> Change
                  </button>
                )}
              </td>

              <td>
                <button
                  className="btn-small delete"
                  onClick={() => handleDeleteDevice(device.credentialId)}
                >
                  <FaTrash /> Delete
                </button>
              </td>
            </tr>
          ))}

          <tr>
            <td colSpan={3}>
              <button className="btn-add-device" onClick={() => setShowAddModal(true)}>
                <FaPlus /> Add Device
              </button>
            </td>
          </tr>
        </tbody>
      </table>


      {/* ADD DEVICE MODAL */}
      {showAddModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>Add New Device</h3>

            <label>Device Name</label>
            <input
              type="text"
              value={addDeviceName}
              onChange={(e) => setAddDeviceName(e.target.value)}
              placeholder="Enter device name"
            />

            <div className="modal-actions">
              <button
                className="btn-primary"
                onClick={handleGenerateNewDevice}
                disabled={loading}
              >
                {loading ? "Generating..." : "Generate Passkey"}
              </button>

              <button
                className="btn-secondary"
                onClick={() => setShowAddModal(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="delete-account modern-delete">
        <button className="btn-danger" onClick={handleDeleteAccount}>
          DELETE ACCOUNT
        </button>
      </div>
    </div>
  );
}
